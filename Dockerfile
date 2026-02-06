# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy everything first to ensure we have the folder structure
COPY . .

# Find any .csproj file and restore dependencies
# This is more flexible than hardcoding the path
RUN dotnet restore

# Build and Publish
# We use the glob pattern to find the project and publish it
RUN dotnet publish **/Social.csproj -c Release -o /app/publish

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user
RUN useradd -m -u 1000 -s /bin/bash dotnetuser
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Fix permissions for the non-root user
RUN chown -R dotnetuser:dotnetuser /app
USER dotnetuser

# Configure Ports for Fly.io
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

# Performance features
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_gcServer=1

# Ensure this matches your actual DLL name
ENTRYPOINT ["dotnet", "Social.dll"]
