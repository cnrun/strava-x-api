# strava-x-api
Release Strava Api limitations by using the web frontend functionalities with Selenium.

From empty DB:
1) dotnet ef migrations add InitialCreate
2) dotnet ef database update

export CONNECTION_STRING=[YOUR_CONNECTION_STRING]
export STRAVA_USER=[YOUR_STRAVA_USER]
export STRAVA_PWD=[YOUT_STRAVA_PWD]

A) Read all Athlete Connections:
dotnet run -- -c=get-athletes --athleteid=123456



B) Retrieve all range queries
create Queries for all athlete in the database:
dotnet run -- -c=get-queries



[![Build Status](https://dev.azure.com/cnrun/strava-x-api/_apis/build/status/cnrun.strava-x-api?branchName=master)](https://dev.azure.com/cnrun/strava-x-api/_build/latest?definitionId=1&branchName=master)
Automation pipeline bei Azure

[![Actions Status](https://github.com/cnrun/strava-x-api/workflows/Docker%20Image%20CI/badge.svg)](https://github.com/cnrun/strava-x-api/actions) Container image push

[![BrowserStack Status](https://automate.browserstack.com/badge.svg?badge_key=TWVsNS9xNTZqOTU3Ym5Ib01wajhwYmwveVEvMDlLM2VvRjBxR0hFNHJuZz0tLVdMS0lBN2ZXUStKNnhQdDFQZjNYc1E9PQ==--89c799dc327edb8160a73792a43956528a850c51)](https://automate.browserstack.com/public-build/TWVsNS9xNTZqOTU3Ym5Ib01wajhwYmwveVEvMDlLM2VvRjBxR0hFNHJuZz0tLVdMS0lBN2ZXUStKNnhQdDFQZjNYc1E9PQ==--89c799dc327edb8160a73792a43956528a850c51) Thanks to BrowserStack for supporting open source and support the resilience of this project.
![BrowserStack Logo](https://d98b8t1nnulk5.cloudfront.net/production/images/layout/logo-header.png?1469004780)
