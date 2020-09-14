# Build:
# Erics-MacBook-Air:strava-x-api ericlouvard$ docker build -t strava-x-api:chrome -f Prototype-Chrome.Dockerfile .
# Run:
# docker run --rm -v /.../data:/app/data strava-x-api:chrome -c=stats
# Run with Selenium
# docker run --rm -v /.../data:/app/data -e "STRAVA_USER=..." -e "STRAVA_PWD=..." -e "FROM_YEAR=2019" -e "FROM_MONTH=08" -e "TO_YEAR=2019" -e "TO_MONTH=08" strava-x-api:chrome -c=get-activities --athleteid=...

# Dockerfile from https://github.com/dotnet/dotnet-docker/blob/master/samples/dotnetapp/Dockerfile
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.402 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY Strava.XApi/*.csproj ./Strava.XApi/
WORKDIR /app/Strava.XApi
RUN dotnet restore

# copy and publish app and libraries
WORKDIR /app/
COPY Strava.XApi/. ./Strava.XApi/
WORKDIR /app/Strava.XApi
RUN dotnet publish -c Release -o out

# test application -- see: dotnet-docker-unit-testing.md
# FROM build AS testrunner
# WORKDIR /app/tests
# COPY tests/. .
# ENTRYPOINT ["dotnet", "test", "--logger:trx"]

FROM mcr.microsoft.com/dotnet/core/runtime:3.1.8 AS runtime
WORKDIR /app
COPY --from=build /app/Strava.XApi/out ./

# Install Chrome for Selenium
# https://stackoverflow.com/a/51266278/281188
# from Selenium:
# https://github.com/SeleniumHQ/docker-selenium/blob/master/Base/Dockerfile
# https://github.com/SeleniumHQ/docker-selenium/blob/master/NodeBase/Dockerfile
# https://github.com/SeleniumHQ/docker-selenium/blob/master/NodeChrome/Dockerfile
# install google chrome
RUN apt-get update && apt-get install -y gnupg2 && apt-get install -y wget \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - \
    && sh -c 'echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google-chrome.list' \
    && apt-get update -qqy \
    && apt-get -qqy install google-chrome-stable

# install chromedriver
RUN apt-get install -yqq unzip \
    && wget -O /tmp/chromedriver.zip http://chromedriver.storage.googleapis.com/`curl -sS chromedriver.storage.googleapis.com/LATEST_RELEASE`/chromedriver_linux64.zip \
    && unzip /tmp/chromedriver.zip chromedriver -d /usr/local/bin/ \
    && rm /tmp/chromedriver.zip

# set display port to avoid crash
ENV DISPLAY :99.0
ENV START_XVFB false

ENTRYPOINT ["dotnet", "StravaXApi.dll"]