# STAGE 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Copy everything first to avoid "path not found" errors
COPY . .

# 2. Restore dependencies 
# This looks for the .csproj file automatically inside the Social folder
RUN dotnet restore "Social/Social.csproj"

# 3. Build and Publish
# We move into the project folder to ensure the build context is correct
WORKDIR "/src/Social"
RUN dotnet publish "Social.csproj" -c Release -o /app/publish /p:UseAppHost=false

# STAGE 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Fly.io requirements: Listen on 8080
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Turn off Globalization invariant if you use Culture-specific logic (optional)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy the build output
COPY --from=build /app/publish .

# The entry point must match your project output DLL name
ENTRYPOINT ["dotnet", "Social.dll"]
