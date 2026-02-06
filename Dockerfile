# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Set the working directory
WORKDIR /src

# Copy the specific csproj from the Social folder
# This matches your project structure: Social/Social.csproj
COPY ["Social/Social.csproj", "Social/"]
RUN dotnet restore "Social/Social.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
# We move to the project folder to publish
WORKDIR "/src/Social"
RUN dotnet publish "Social.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install curl for health checks (useful for Fly.io)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN useradd -m -u 1000 -s /bin/bash dotnetuser

# Set the working directory
WORKDIR /app

# Copy the published application
COPY --from=build /app/publish .

# Change ownership
RUN chown -R dotnetuser:dotnetuser /app

# Switch to the non-root user
USER dotnetuser

# Configure Ports for Fly.io
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

# Performance features
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_gcServer=1

# Ensure the DLL name matches your project
ENTRYPOINT ["dotnet", "Social.dll"]
