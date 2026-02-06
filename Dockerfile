# Stage 1: Runtime Base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Fly.io uses 8080 by default
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only the project file first (to optimize caching)
# Note: We point to the file inside the Social folder
COPY ["Social/Social.csproj", "Social/"]
RUN dotnet restore "Social/Social.csproj"

# Copy everything else
COPY . .
WORKDIR "/src/Social"
RUN dotnet build "Social.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Stage 3: Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Social.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Social.dll"]
