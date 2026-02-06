# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy all files
COPY . .

# Restore dependencies
# The logs showed this works fine because it finds the .sln file automatically
RUN dotnet restore

# Publish the API project
# FIX: Adjusted path to point to 'Social.API.csproj' based on your logs
RUN dotnet publish "Social/Social.API.csproj" -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install utilities for Fly.io health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Setup non-root user
RUN useradd -m -u 1000 -s /bin/bash dotnetuser
WORKDIR /app

# Copy the build artifacts
COPY --from=build /app/publish .

# Permissions
RUN chown -R dotnetuser:dotnetuser /app
USER dotnetuser

# Configure Environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

# Performance Tuning
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_gcServer=1

# FIX: The DLL usually matches the project name. 
# Since the project is Social.API.csproj, the DLL is Social.API.dll
ENTRYPOINT ["dotnet", "Social.API.dll"]
