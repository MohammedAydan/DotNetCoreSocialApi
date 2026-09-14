<!-- Create Migration -->
dotnet ef migrations add InitialCreateBlockUserTable --project .\Social.Infrastructure\Social.Infrastructure.csproj --startup-project .\Social\Social.Api.csproj

<!-- Update Database -->
dotnet ef database update --project .\Social.Infrastructure\Social.Infrastructure.csproj --startup-project .\Social\Social.Api.csproj
