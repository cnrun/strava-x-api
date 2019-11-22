# Build:
# Erics-MacBook-Air:strava-x-api ericlouvard$ docker build -t strava-x-api:latest -f Prototype.Dockerfile .
# Run:
# docker run --rm -v /Users/ericlouvard/Documents/Projects/temp/data:/app/data strava-x-api -c=stats

# Dockerfile from https://github.com/dotnet/dotnet-docker/blob/master/samples/dotnetapp/Dockerfile
FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS build
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

FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS runtime
WORKDIR /app
COPY --from=build /app/Prototype/out ./

# Install Chrome for Selenium
# https://stackoverflow.com/a/51266278/281188
# install google chrome
RUN apt-get update && apt-get install -y gnupg2 && apt-get install -y wget
RUN wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add -
RUN sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list'
RUN apt-get -y update
RUN apt-get install -y google-chrome-stable

# install chromedriver
RUN apt-get install -yqq unzip
RUN wget -O /tmp/chromedriver.zip http://chromedriver.storage.googleapis.com/`curl -sS chromedriver.storage.googleapis.com/LATEST_RELEASE`/chromedriver_linux64.zip
RUN unzip /tmp/chromedriver.zip chromedriver -d /usr/local/bin/

# set display port to avoid crash
ENV DISPLAY=:99

ENTRYPOINT ["dotnet", "Prototype.dll"]