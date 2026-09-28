FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY CarminePeople.sln .
COPY src/CarminePeople.Domain/CarminePeople.Domain.csproj src/CarminePeople.Domain/
COPY src/CarminePeople.Application/CarminePeople.Application.csproj src/CarminePeople.Application/
COPY src/CarminePeople.Infrastructure/CarminePeople.Infrastructure.csproj src/CarminePeople.Infrastructure/
COPY src/CarminePeople.Api/CarminePeople.Api.csproj src/CarminePeople.Api/
COPY tests/CarminePeople.UnitTests/CarminePeople.UnitTests.csproj tests/CarminePeople.UnitTests/
COPY tests/CarminePeople.IntegrationTests/CarminePeople.IntegrationTests.csproj tests/CarminePeople.IntegrationTests/
RUN dotnet restore src/CarminePeople.Api/CarminePeople.Api.csproj

COPY . .
RUN dotnet publish src/CarminePeople.Api/CarminePeople.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CarminePeople.Api.dll"]
