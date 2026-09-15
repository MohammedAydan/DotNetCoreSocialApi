#requires -Version 5.1

<##
.SYNOPSIS
    Build, publish, and deploy an ASP.NET Core application to IIS over FTP.

.DESCRIPTION
    - Validates configuration and local prerequisites.
    - Cleans and publishes the ASP.NET Core project in Release mode.
    - Validates the publish output.
    - Uploads app_offline.htm first.
    - Creates missing FTP directories recursively.
    - Uploads all publish files except explicitly excluded files.
    - Removes app_offline.htm only after the upload completes successfully.
    - Performs an HTTP health check.
    - If the health check fails, puts the application offline again.
    - Returns exit code 0 on success and 1 on failure.

.NOTES
    Set FTP credentials before running:

        $env:DEPLOY_FTP_USER = "YOUR_FTP_USERNAME"
        $env:DEPLOY_FTP_PASSWORD = "YOUR_FTP_PASSWORD"

    Then:

        Unblock-File .\deploy.ps1
        Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
        .\deploy.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ============================================================
# CONFIGURATION
# ============================================================

$ProjectFile = '.\Social\Social.API.csproj'
$PublishDir = Join-Path ([System.IO.Path]::GetTempPath()) ('SocialApi-Publish-' + [System.Guid]::NewGuid().ToString('N'))

# FTP root must be the IIS application's FTP directory.
$FtpBaseUri = 'ftp://site26082.siteasp.net/'

# Optional FTPS.
$UseFtps = $false

# Public URL used to verify the application after deployment.
# Prefer a dedicated health endpoint if your API has one.
$HealthCheckUrl = 'https://site26082.siteasp.net/'

$HealthCheckAttempts = 12
$HealthCheckDelaySeconds = 5
$HealthCheckTimeoutSeconds = 15

$Configuration = 'Release'

# Files that must never be deployed.
$ExcludedFileNames = @(
    'appsettings.Development.json',
    'app_offline.htm'
)

# FTP retry behavior.
$FtpRetryCount = 3
$FtpRetryDelaySeconds = 2

# ============================================================
# LOGGING
# ============================================================

function Write-Info {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[ OK ] $Message" -ForegroundColor Green
}

function Write-WarningMessage {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-ErrorMessage {
    param([Parameter(Mandatory = $true)][string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

# ============================================================
# COMMAND EXECUTION
# ============================================================

function Invoke-CommandChecked {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $false)]
        [string[]]$Arguments = @(),

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    Write-Info $Description

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }

    Write-Success "$Description completed."
}

# ============================================================
# FTP REQUEST
# ============================================================

function New-FtpRequest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Uri,

        [Parameter(Mandatory = $true)]
        [string]$Method
    )

    $request = [System.Net.FtpWebRequest]::Create($Uri)

    $request.Method = $Method
    $request.Credentials = New-Object System.Net.NetworkCredential(
        $script:FtpUser,
        $script:FtpPassword
    )

    $request.UseBinary = $true
    $request.UsePassive = $true
    $request.KeepAlive = $false
    $request.Timeout = 60000
    $request.ReadWriteTimeout = 60000

    if ($script:UseFtps) {
        $request.EnableSsl = $true
    }

    return $request
}

# ============================================================
# FTP DIRECTORY HELPERS
# ============================================================

function Test-FtpRemoteDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RemoteUri
    )

    $request = $null
    $response = $null

    try {
        $request = New-FtpRequest `
            -Uri $RemoteUri `
            -Method ([System.Net.WebRequestMethods+Ftp]::ListDirectory)

        $response = $request.GetResponse()
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($null -ne $response) {
            $response.Close()
        }
    }
}

function Ensure-FtpDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RemoteDirectory
    )

    $directoryUri = $RemoteDirectory.TrimEnd('/') + '/'

    if (Test-FtpRemoteDirectory -RemoteUri $directoryUri) {
        return
    }

    $request = $null
    $response = $null

    try {
        Write-Info "Creating FTP directory: $directoryUri"

        $request = New-FtpRequest `
            -Uri $directoryUri `
            -Method ([System.Net.WebRequestMethods+Ftp]::MakeDirectory)

        $response = $request.GetResponse()
    }
    catch {
        # Some FTP servers deny LIST even when a directory exists.
        # Re-check before treating this as a real failure.
        if (-not (Test-FtpRemoteDirectory -RemoteUri $directoryUri)) {
            throw "Unable to create/access FTP directory '$directoryUri'. $($_.Exception.Message)"
        }
    }
    finally {
        if ($null -ne $response) {
            $response.Close()
        }
    }
}

function Ensure-FtpDirectoryTree {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RemoteDirectory
    )

    try {
        $uri = [System.Uri]$RemoteDirectory
    }
    catch {
        throw "Invalid FTP URI: $RemoteDirectory"
    }

    if ($uri.Scheme -ne 'ftp') {
        throw "Only ftp:// URIs are supported by this script: $RemoteDirectory"
    }

    $serverName = $uri.Host
    $pathParts = $uri.AbsolutePath.Split(
        '/',
        [System.StringSplitOptions]::RemoveEmptyEntries
    )

    if ($pathParts.Count -eq 0) {
        return
    }

    $currentUri = "ftp://$serverName/"

    foreach ($pathPart in $pathParts) {
        $currentUri = $currentUri.TrimEnd('/') + '/' + $pathPart + '/'
        Ensure-FtpDirectory -RemoteDirectory $currentUri
    }
}

# ============================================================
# FTP FILE UPLOAD
# ============================================================

function Upload-FtpFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$LocalFile,

        [Parameter(Mandatory = $true)]
        [string]$RemoteUri
    )

    if (-not (Test-Path -LiteralPath $LocalFile -PathType Leaf)) {
        throw "Local file not found: $LocalFile"
    }

    for ($attempt = 1; $attempt -le $script:FtpRetryCount; $attempt++) {
        $request = $null
        $inputStream = $null
        $outputStream = $null
        $response = $null

        try {
            $request = New-FtpRequest `
                -Uri $RemoteUri `
                -Method ([System.Net.WebRequestMethods+Ftp]::UploadFile)

            $fileInfo = Get-Item -LiteralPath $LocalFile
            $request.ContentLength = $fileInfo.Length

            $inputStream = [System.IO.File]::OpenRead($LocalFile)
            $outputStream = $request.GetRequestStream()

            $buffer = New-Object byte[] 65536
            while (($bytesRead = $inputStream.Read($buffer, 0, $buffer.Length)) -gt 0) {
                $outputStream.Write($buffer, 0, $bytesRead)
            }

            $outputStream.Close()
            $outputStream.Dispose()
            $outputStream = $null

            $inputStream.Close()
            $inputStream.Dispose()
            $inputStream = $null

            $response = $request.GetResponse()
            return
        }
        catch {
            if ($attempt -eq $script:FtpRetryCount) {
                throw "FTP upload failed.`nLocal: $LocalFile`nRemote: $RemoteUri`nError: $($_.Exception.Message)"
            }

            Write-WarningMessage "FTP upload failed for '$LocalFile'. Retry ${attempt}/$($script:FtpRetryCount)..."
            Start-Sleep -Seconds $script:FtpRetryDelaySeconds
        }
        finally {
            if ($null -ne $outputStream) {
                $outputStream.Close()
                $outputStream.Dispose()
            }

            if ($null -ne $inputStream) {
                $inputStream.Close()
                $inputStream.Dispose()
            }

            if ($null -ne $response) {
                $response.Close()
            }
        }
    }
}

# ============================================================
# FTP DELETE
# ============================================================

function Remove-FtpFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RemoteUri
    )

    $request = $null
    $response = $null

    try {
        $request = New-FtpRequest `
            -Uri $RemoteUri `
            -Method ([System.Net.WebRequestMethods+Ftp]::DeleteFile)

        $response = $request.GetResponse()
        Write-Success "Removed remote file: $RemoteUri"
    }
    catch {
        # FTP 550 generally means the file does not exist.
        if ($_.Exception.Message -match '550') {
            Write-WarningMessage "Remote file does not exist: $RemoteUri"
            return
        }

        throw "Unable to delete remote file '$RemoteUri'. $($_.Exception.Message)"
    }
    finally {
        if ($null -ne $response) {
            $response.Close()
        }
    }
}

# ============================================================
# HTTP HEALTH CHECK
# ============================================================

function Test-ApplicationHealth {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Url
    )

    Write-Info "Running HTTP health check: $Url"

    for ($attempt = 1; $attempt -le $script:HealthCheckAttempts; $attempt++) {
        try {
            $response = Invoke-WebRequest `
                -Uri $Url `
                -Method GET `
                -UseBasicParsing `
                -TimeoutSec $script:HealthCheckTimeoutSeconds `
                -ErrorAction Stop

            $statusCode = [int]$response.StatusCode

            if ($statusCode -ge 200 -and $statusCode -lt 400) {
                Write-Success "Health check passed. HTTP $statusCode."
                return $true
            }

            Write-WarningMessage "Health check returned HTTP $statusCode on attempt ${attempt}/$($script:HealthCheckAttempts)."
        }
        catch {
            Write-WarningMessage "Health check attempt ${attempt}/$($script:HealthCheckAttempts) failed: $($_.Exception.Message)"
        }

        if ($attempt -lt $script:HealthCheckAttempts) {
            Start-Sleep -Seconds $script:HealthCheckDelaySeconds
        }
    }

    return $false
}

# ============================================================
# MAIN
# ============================================================

$script:FtpUser = $env:DEPLOY_FTP_USER
$script:FtpPassword = $env:DEPLOY_FTP_PASSWORD

Write-Host ''
Write-Host '============================================================' -ForegroundColor DarkCyan
Write-Host ' Social API - Production FTP Deployment' -ForegroundColor White
Write-Host '============================================================' -ForegroundColor DarkCyan
Write-Host ''

$offlineFile = Join-Path $PublishDir 'app_offline.htm'
$remoteOfflineUri = $FtpBaseUri.TrimEnd('/') + '/app_offline.htm'
$deploymentStarted = $false
$deploymentSucceeded = $false

try {
    # --------------------------------------------------------
    # Validate local environment
    # --------------------------------------------------------

    Write-Info 'Validating deployment environment...'

    if ($PSVersionTable.PSVersion.Major -lt 5) {
        throw 'PowerShell 5.1 or newer is required.'
    }

    if (-not (Test-Path -LiteralPath $ProjectFile -PathType Leaf)) {
        throw "Project file not found: $ProjectFile"
    }

    if ([string]::IsNullOrWhiteSpace($script:FtpUser)) {
        throw 'DEPLOY_FTP_USER environment variable is not set.'
    }

    if ([string]::IsNullOrWhiteSpace($script:FtpPassword)) {
        throw 'DEPLOY_FTP_PASSWORD environment variable is not set.'
    }

    try {
        $dotnetVersion = (& dotnet --version).Trim()
    }
    catch {
        throw 'dotnet SDK was not found in PATH.'
    }

    Write-Success "dotnet SDK: $dotnetVersion"
    Write-Success "FTP host: $($FtpBaseUri.TrimEnd('/') + '/')"

    # --------------------------------------------------------
    # Clean publish directory
    # --------------------------------------------------------

    Write-Info 'Preparing publish directory...'

    if (Test-Path -LiteralPath $PublishDir) {
        Remove-Item -LiteralPath $PublishDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $PublishDir -Force | Out-Null

    Write-Success 'Publish directory ready.'

    # --------------------------------------------------------
    # Restore
    # --------------------------------------------------------

    Invoke-CommandChecked `
        -FilePath 'dotnet' `
        -Arguments @(
            'restore',
            $ProjectFile
        ) `
        -Description 'Restoring NuGet packages'

    # --------------------------------------------------------
    # Build
    # --------------------------------------------------------

    Invoke-CommandChecked `
        -FilePath 'dotnet' `
        -Arguments @(
            'build',
            $ProjectFile,
            '--configuration',
            $Configuration,
            '--no-restore'
        ) `
        -Description 'Building application'

    # --------------------------------------------------------
    # Publish
    # --------------------------------------------------------

    Invoke-CommandChecked `
        -FilePath 'dotnet' `
        -Arguments @(
            'publish',
            $ProjectFile,
            '--configuration',
            $Configuration,
            '--no-build',
            '--output',
            $PublishDir
        ) `
        -Description 'Publishing application'

    # --------------------------------------------------------
    # Validate publish output
    # --------------------------------------------------------

    Write-Info 'Validating publish output...'

    $requiredFiles = @(
        'Social.API.dll',
        'Social.API.deps.json',
        'Social.API.runtimeconfig.json',
        'web.config'
    )

    foreach ($requiredFile in $requiredFiles) {
        $requiredPath = Join-Path $PublishDir $requiredFile

        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "Required publish file is missing: $requiredFile"
        }
    }

    Write-Success 'Publish output validated.'

    # --------------------------------------------------------
    # Create app_offline.htm
    # --------------------------------------------------------

    @'
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Maintenance</title>
    <style>
        body {
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            background: #111827;
            color: #fff;
            font-family: Arial, sans-serif;
            text-align: center;
        }
        main {
            max-width: 620px;
            padding: 40px 24px;
        }
        p {
            color: #cbd5e1;
            line-height: 1.6;
        }
    </style>
</head>
<body>
    <main>
        <h1>Updating application</h1>
        <p>The website is temporarily unavailable while a new version is being deployed.</p>
        <p>Please try again shortly.</p>
    </main>
</body>
</html>
'@ | Set-Content -LiteralPath $offlineFile -Encoding UTF8

    # --------------------------------------------------------
    # Collect files
    # --------------------------------------------------------

    $publishRoot = (Resolve-Path -LiteralPath $PublishDir).Path

    $allFiles = Get-ChildItem `
        -LiteralPath $PublishDir `
        -Recurse `
        -File

    $filesToUpload = New-Object System.Collections.Generic.List[object]
    $excludedCount = 0

    foreach ($file in $allFiles) {
        if ($ExcludedFileNames -contains $file.Name) {
            $excludedCount++
            continue
        }

        $relativePath = $file.FullName.Substring($publishRoot.Length).TrimStart('\', '/')

        if ($relativePath -match '^(?:publish/)+') {
            throw "Unexpected nested publish output detected: $relativePath"
        }

        [void]$filesToUpload.Add(
            [PSCustomObject]@{
                LocalFile = $file.FullName
                RelativePath = $relativePath.Replace('\', '/')
            }
        )
    }

    Write-Info "Excluded files: $excludedCount"
    Write-Info "Files to upload: $($filesToUpload.Count)"

    # --------------------------------------------------------
    # Put application offline
    # --------------------------------------------------------

    Write-Host ''
    Write-Info 'Uploading app_offline.htm...'

    Upload-FtpFile `
        -LocalFile $offlineFile `
        -RemoteUri $remoteOfflineUri

    $deploymentStarted = $true

    Write-Success 'Application is now offline.'

    # --------------------------------------------------------
    # Create remote directories
    # --------------------------------------------------------

    Write-Info 'Preparing remote directories...'

    $directories = @{}

    foreach ($file in $filesToUpload) {
        $directory = Split-Path -Path $file.RelativePath -Parent

        if (-not [string]::IsNullOrWhiteSpace($directory) -and $directory -ne '.') {
            $normalizedDirectory = $directory.Replace('\', '/').Trim('/')

            if (-not [string]::IsNullOrWhiteSpace($normalizedDirectory)) {
                $directories[$normalizedDirectory] = $true
            }
        }
    }

    foreach ($directory in ($directories.Keys | Sort-Object)) {
        $remoteDirectory = $FtpBaseUri.TrimEnd('/') + '/' + $directory
        Ensure-FtpDirectoryTree -RemoteDirectory $remoteDirectory
    }

    Write-Success 'Remote directories ready.'

    # --------------------------------------------------------
    # Upload files
    # --------------------------------------------------------

    Write-Host ''
    Write-Info 'Uploading application files...'

    $totalFiles = $filesToUpload.Count
    $counter = 0

    foreach ($file in $filesToUpload) {
        $counter++

        $remoteUri = $FtpBaseUri.TrimEnd('/') + '/' + $file.RelativePath

        Write-Host ("[{0}/{1}] {2}" -f $counter, $totalFiles, $file.RelativePath) -ForegroundColor Gray

        Upload-FtpFile `
            -LocalFile $file.LocalFile `
            -RemoteUri $remoteUri
    }

    Write-Success 'All application files uploaded.'

    # --------------------------------------------------------
    # Remove offline file
    # --------------------------------------------------------

    Write-Info 'Removing app_offline.htm...'

    Remove-FtpFile -RemoteUri $remoteOfflineUri

    Write-Success 'Application startup requested.'

    # --------------------------------------------------------
    # Health check
    # --------------------------------------------------------

    if (-not (Test-ApplicationHealth -Url $HealthCheckUrl)) {
        throw 'Deployment completed, but the application failed the HTTP health check.'
    }

    $deploymentSucceeded = $true

    Write-Host ''
    Write-Host '============================================================' -ForegroundColor Green
    Write-Host ' DEPLOYMENT SUCCESSFUL' -ForegroundColor Green
    Write-Host '============================================================' -ForegroundColor Green
    Write-Host ''
}
catch {
    Write-Host ''
    Write-Host '============================================================' -ForegroundColor Red
    Write-Host ' DEPLOYMENT FAILED' -ForegroundColor Red
    Write-Host '============================================================' -ForegroundColor Red
    Write-ErrorMessage $_.Exception.Message

    # --------------------------------------------------------
    # Safety: if deployment started and health check failed,
    # put the site offline again.
    # --------------------------------------------------------

    if ($deploymentStarted -and -not $deploymentSucceeded) {
        try {
            if (Test-Path -LiteralPath $offlineFile -PathType Leaf) {
                Upload-FtpFile `
                    -LocalFile $offlineFile `
                    -RemoteUri $remoteOfflineUri

                Write-WarningMessage 'Application was placed offline after deployment failure.'
            }
        }
        catch {
            Write-ErrorMessage "Could not place the failed deployment offline: $($_.Exception.Message)"
        }
    }

    exit 1
}
finally {
    if (Test-Path -LiteralPath $offlineFile) {
        Remove-Item -LiteralPath $offlineFile -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $PublishDir) {
        Remove-Item -LiteralPath $PublishDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($deploymentSucceeded) {
    exit 0
}

exit 1
