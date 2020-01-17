# Build:
# Erics-MacBook-Air:strava-x-api ericlouvard$ docker build -t strava-x-api:latest -f Prototype.Dockerfile .
# Run:
# docker run --rm -v /Users/ericlouvard/Documents/Projects/temp/data:/app/data strava-x-api -c=stats

# Dockerfile from https://github.com/dotnet/dotnet-docker/blob/master/samples/dotnetapp/Dockerfile
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY Prototype/*.csproj ./Prototype/
WORKDIR /app/Prototype
RUN dotnet restore

# copy and publish app and libraries
WORKDIR /app/
COPY Prototype/. ./Prototype/
WORKDIR /app/Prototype
RUN dotnet publish -c Release -o out

# test application -- see: dotnet-docker-unit-testing.md
# FROM build AS testrunner
# WORKDIR /app/tests
# COPY tests/. .
# ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/Prototype/out ./
ENTRYPOINT ["dotnet", "Prototype.dll"]