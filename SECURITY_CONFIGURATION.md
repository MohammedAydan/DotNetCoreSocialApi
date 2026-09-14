# Secure Configuration Guide

This document outlines best practices for securely managing sensitive configuration data such as connection strings, API keys, and JWT secrets.

## Overview

The Social API requires several sensitive configuration values:
- Database connection string with credentials
- JWT signing key, issuer, and audience
- API key for external access
- Email SMTP credentials (optional)
- Redis connection credentials (optional)

**NEVER** commit these values to version control. Always use secure configuration methods appropriate for your environment.

## Configuration Hierarchy

The application loads configuration in this order (later sources override earlier ones):

1. `appsettings.json` - Non-sensitive defaults
2. `appsettings.{Environment}.json` - Environment-specific defaults
3. `.env` file - Local development secrets (gitignored)
4. Environment variables - System or container environment
5. User Secrets - .NET development tool
6. Azure Key Vault / Cloud secret managers - Production

## Development Environment

### Option 1: .env File (Recommended for Local Development)

1. Create a `.env` file in the project root (already gitignored):

```env
# Database
CONNECTION_STRING=Server=localhost;Port=3306;Database=SocialDb;User=root;Password=dev_password;

# JWT Configuration
JWT_KEY=your-super-secret-jwt-key-min-32-characters
JWT_ISSUER=https://localhost:5000
JWT_AUDIENCE=https://localhost:3000
JWT_EXPIRE_TIME=30

# API Security
API_KEY=your-api-key-for-external-services

# Frontend
FRONTEND_URL=http://localhost:3000

# Email (Optional)
EMAIL_SMTP_SERVER=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SENDER_NAME=Social API
EMAIL_SENDER_EMAIL=noreply@example.com
EMAIL_USERNAME=your-email@gmail.com
EMAIL_PASSWORD=your-app-password
EMAIL_ENABLE_SSL=true

# Redis (Optional)
REDIS_ENDPOINT=localhost:6379
REDIS_USERNAME=
REDIS_PASSWORD=
```

2. The application automatically loads `.env` on startup using DotNetEnv package

3. ✅ **Advantages**:
   - Simple to use
   - Works across different development machines
   - Can be templated in `.env.example`
   - Already gitignored

### Option 2: User Secrets (.NET Tool)

1. Initialize user secrets (one-time per project):
```bash
cd Social
dotnet user-secrets init
```

2. Set individual secrets:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=SocialDb;User=root;Password=dev_password;"
dotnet user-secrets set "Jwt:Key" "your-super-secret-jwt-key-min-32-characters"
dotnet user-secrets set "Jwt:Issuer" "https://localhost:5000"
dotnet user-secrets set "Jwt:Audience" "https://localhost:3000"
dotnet user-secrets set "ApiSettings:ApiKey" "your-api-key"
dotnet user-secrets set "GenralConfig:FrontendUrl" "http://localhost:3000"
```

3. View all secrets:
```bash
dotnet user-secrets list
```

4. ✅ **Advantages**:
   - Stored outside project directory
   - Built into .NET tooling
   - Works seamlessly with Visual Studio
   - Per-user configuration

### Option 3: Environment Variables (System-Wide)

**Windows (PowerShell)**:
```powershell
$env:CONNECTION_STRING="Server=localhost;Port=3306;Database=SocialDb;User=root;Password=dev_password;"
$env:JWT_KEY="your-super-secret-jwt-key-min-32-characters"
$env:JWT_ISSUER="https://localhost:5000"
$env:JWT_AUDIENCE="https://localhost:3000"
$env:API_KEY="your-api-key"
$env:FRONTEND_URL="http://localhost:3000"
```

**Linux/Mac (Bash)**:
```bash
export CONNECTION_STRING="Server=localhost;Port=3306;Database=SocialDb;User=root;Password=dev_password;"
export JWT_KEY="your-super-secret-jwt-key-min-32-characters"
export JWT_ISSUER="https://localhost:5000"
export JWT_AUDIENCE="https://localhost:3000"
export API_KEY="your-api-key"
export FRONTEND_URL="http://localhost:3000"
```

4. For persistent environment variables:
   - **Windows**: System Properties → Environment Variables
   - **Linux**: Add to `~/.bashrc` or `~/.zshrc`
   - **Mac**: Add to `~/.bash_profile` or `~/.zshrc`

## Staging/Testing Environment

### Docker Compose with Environment File

1. Create `docker-compose.yml`:
```yaml
version: '3.8'
services:
  social-api:
    image: social-api:latest
    env_file:
      - .env.staging
    ports:
      - "5000:80"
    depends_on:
      - mysql
      - redis

  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: ${MYSQL_ROOT_PASSWORD}
      MYSQL_DATABASE: SocialDb
    volumes:
      - mysql-data:/var/lib/mysql

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass ${REDIS_PASSWORD}

volumes:
  mysql-data:
```

2. Create `.env.staging` (gitignored):
```env
CONNECTION_STRING=Server=mysql;Port=3306;Database=SocialDb;User=root;Password=${MYSQL_ROOT_PASSWORD};
JWT_KEY=staging-jwt-key-replace-with-secure-value
JWT_ISSUER=https://api.staging.example.com
JWT_AUDIENCE=https://staging.example.com
JWT_EXPIRE_TIME=30
API_KEY=staging-api-key
FRONTEND_URL=https://staging.example.com
REDIS_ENDPOINT=redis:6379
REDIS_PASSWORD=${REDIS_PASSWORD}
MYSQL_ROOT_PASSWORD=staging_mysql_password
```

3. Deploy:
```bash
docker-compose --env-file .env.staging up -d
```

## Production Environment

### Option 1: Azure App Service Configuration

1. Navigate to Azure Portal → App Service → Configuration

2. Add Application Settings:
```
Name: CONNECTION_STRING
Value: Server=prod-mysql.database.azure.com;Port=3306;Database=SocialDb;User=admin@prod-mysql;Password=***;SslMode=Required;

Name: JWT__KEY
Value: *** (strong random key)

Name: JWT__ISSUER
Value: https://api.production.example.com

Name: JWT__AUDIENCE
Value: https://production.example.com

Name: API_KEY
Value: *** (secure API key)

Name: FRONTEND_URL
Value: https://production.example.com

Name: REDIS__ENDPOINT
Value: prod-redis.redis.cache.windows.net:6380

Name: REDIS__PASSWORD
Value: *** (Redis access key)
```

3. Enable "Deployment slot settings" to prevent accidental overwrites

4. ✅ **Advantages**:
   - Managed by Azure
   - Encrypted at rest
   - Access controlled via RBAC
   - Audit logging

### Option 2: Azure Key Vault (Recommended for Production)

1. Create Key Vault:
```bash
az keyvault create --name social-api-kv --resource-group social-api-rg --location eastus
```

2. Add secrets:
```bash
az keyvault secret set --vault-name social-api-kv --name ConnectionString --value "Server=..."
az keyvault secret set --vault-name social-api-kv --name JwtKey --value "***"
az keyvault secret set --vault-name social-api-kv --name ApiKey --value "***"
```

3. Grant App Service access:
```bash
# Enable managed identity on App Service
az webapp identity assign --name social-api --resource-group social-api-rg

# Grant access to Key Vault
az keyvault set-policy --name social-api-kv --object-id <managed-identity-id> --secret-permissions get list
```

4. Update `Program.cs`:
```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultUrl = new Uri(builder.Configuration["KeyVault:Url"]
        ?? throw new InvalidOperationException("KeyVault URL not configured"));

    builder.Configuration.AddAzureKeyVault(
        keyVaultUrl,
        new DefaultAzureCredential());
}
```

5. Set Key Vault URL in App Service configuration:
```
Name: KeyVault__Url
Value: https://social-api-kv.vault.azure.net/
```

6. ✅ **Advantages**:
   - Centralized secret management
   - Automatic rotation support
   - Comprehensive audit logging
   - Network isolation options
   - RBAC and access policies

### Option 3: Kubernetes Secrets

1. Create secret from literal values:
```bash
kubectl create secret generic social-api-secrets \
  --from-literal=CONNECTION_STRING="Server=..." \
  --from-literal=JWT_KEY="***" \
  --from-literal=API_KEY="***" \
  --namespace=production
```

2. Or from file:
```bash
kubectl create secret generic social-api-secrets \
  --from-env-file=production.env \
  --namespace=production
```

3. Reference in deployment:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: social-api
spec:
  template:
    spec:
      containers:
      - name: api
        image: social-api:latest
        envFrom:
        - secretRef:
            name: social-api-secrets
```

4. ✅ **Advantages**:
   - Native Kubernetes integration
   - Base64 encoded
   - RBAC controlled
   - Can use external secret operators (e.g., External Secrets Operator with AWS Secrets Manager)

### Option 4: AWS Secrets Manager

1. Create secrets:
```bash
aws secretsmanager create-secret \
  --name social-api/connection-string \
  --secret-string "Server=..."

aws secretsmanager create-secret \
  --name social-api/jwt-key \
  --secret-string "***"
```

2. Update `Program.cs`:
```csharp
if (builder.Environment.IsProduction())
{
    builder.Configuration.AddSecretsManager(
        region: RegionEndpoint.USEast1,
        configurator: options =>
        {
            options.SecretFilter = entry => entry.Name.StartsWith("social-api/");
            options.KeyGenerator = (entry, key) => key.Replace("social-api/", "")
                .Replace("-", ":");
        });
}
```

3. Grant EC2/ECS role access to secrets

4. ✅ **Advantages**:
   - Automatic rotation
   - Fine-grained IAM policies
   - CloudTrail audit logging
   - Cross-region replication

## Security Best Practices

### 1. Strong Secrets Generation

**JWT Key** (minimum 32 characters):
```bash
# PowerShell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 64 | ForEach-Object {[char]$_})

# Linux/Mac
openssl rand -base64 64

# Online (use with caution)
# https://generate-secret.now.sh/64
```

**API Key**:
```bash
# UUID-based
uuidgen

# Random hex
openssl rand -hex 32
```

### 2. Connection String Security

❌ **Bad** - Plain text in code:
```csharp
var connectionString = "Server=prod;User=root;Password=MyPassword123!";
```

✅ **Good** - From secure configuration:
```csharp
var connectionString = configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string not configured");
```

✅ **Better** - With validation and masking:
```csharp
var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Connection string is required");

// Log masked version
var masked = Regex.Replace(connectionString, @"(Password=)[^;]*", "$1***");
logger.LogInformation("Using connection: {ConnectionString}", masked);
```

### 3. .gitignore Configuration

Ensure these patterns are in `.gitignore`:
```gitignore
# Environment files
.env
.env.*
!.env.example

# User secrets
**/appsettings.*.json
!**/appsettings.json
!**/appsettings.Development.json

# IDE
.vs/
.vscode/
*.user
*.suo

# Build
obj/
bin/
```

### 4. Template File for Onboarding

Create `.env.example` (safe to commit):
```env
# Database Configuration
CONNECTION_STRING=Server=localhost;Port=3306;Database=SocialDb;User=root;Password=YOUR_PASSWORD

# JWT Configuration
JWT_KEY=YOUR_SECRET_KEY_MIN_32_CHARS
JWT_ISSUER=https://localhost:5000
JWT_AUDIENCE=https://localhost:3000
JWT_EXPIRE_TIME=30

# API Security
API_KEY=YOUR_API_KEY

# Application
FRONTEND_URL=http://localhost:3000

# Email (Optional)
EMAIL_SMTP_SERVER=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SENDER_NAME=Social API
EMAIL_SENDER_EMAIL=noreply@example.com
EMAIL_USERNAME=your-email@example.com
EMAIL_PASSWORD=your-smtp-password
EMAIL_ENABLE_SSL=true

# Redis (Optional)
REDIS_ENDPOINT=localhost:6379
REDIS_USERNAME=
REDIS_PASSWORD=
```

Onboarding instructions:
```bash
# 1. Copy template
cp .env.example .env

# 2. Edit .env and replace placeholder values
nano .env

# 3. Never commit .env file
git status  # Should not show .env
```

### 5. Rotation Strategy

**Development**: Rotate quarterly or when developers leave

**Production**: Rotate monthly or immediately if:
- Key compromise suspected
- Team member with access leaves
- Compliance requirement
- After security incident

**Rotation Process**:
1. Generate new secret
2. Add new secret alongside old (dual-run)
3. Deploy application changes
4. Verify new secret works
5. Remove old secret
6. Update documentation

### 6. Access Control

- **Development**: Only developers who actively need access
- **Staging**: QA team + developers
- **Production**: Only DevOps/SRE team + approved deployers
- Use principle of least privilege
- Audit access logs regularly

### 7. Monitoring and Alerts

Set up alerts for:
- Failed authentication attempts
- Configuration access from unexpected IPs
- Secret rotation failures
- Unauthorized secret access attempts

## Validation Checklist

Before deploying to production, verify:

- [ ] No secrets in source code or committed files
- [ ] `.env` files are gitignored
- [ ] `appsettings.json` contains no sensitive values
- [ ] Production secrets are in secure key vault
- [ ] Connection strings use SSL/TLS
- [ ] JWT keys are cryptographically strong (≥32 chars)
- [ ] API keys are unique per environment
- [ ] Access to secrets is RBAC-controlled
- [ ] Rotation schedule is documented
- [ ] Backup/disaster recovery plan exists
- [ ] Team is trained on secret management

## Troubleshooting

### "Connection string not found"
```bash
# Check environment variables
printenv | grep CONNECTION_STRING

# Check .env file exists
ls -la .env

# Verify .env is loaded (check startup logs)
# Should see: "[✓] .env file loaded successfully"
```

### "JWT key is missing in configuration"
```bash
# Check JWT configuration
printenv | grep JWT

# Verify length (should be ≥32 characters)
echo $JWT_KEY | wc -c
```

### " Redis connection failed"
This is expected if Redis is optional. See warning in logs:
```
[⚠] Failed to connect to Redis: ...
[ℹ] Using in-memory cache fallback.
```

If Redis is required, verify:
```bash
# Check Redis is running
redis-cli ping

# Check endpoint configuration
echo $REDIS_ENDPOINT
```

## Additional Resources

- [ASP.NET Core Secret Manager](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/)
- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/)
- [Kubernetes Secrets](https://kubernetes.io/docs/concepts/configuration/secret/)
- [OWASP Secrets Management](https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html)

---

*Last Updated: February 9, 2026*
*For security concerns, contact the security team immediately*
