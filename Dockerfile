FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore HealthInsurance.slnx
RUN dotnet build HealthInsurance.slnx -nr:false -v:minimal --no-restore

FROM build AS validation
CMD ["bash", "-lc", "dotnet run --project Tests/Tests.csproj --no-build && dotnet run --project UI/UI.csproj --no-build"]

FROM build AS web
ENV ASPNETCORE_URLS=http://+:8080
ENV HEALTH_INSURANCE_DB=/data/health-insurance-web.db
RUN mkdir -p /data
EXPOSE 8080
CMD ["dotnet", "run", "--project", "WebUI/WebUI.csproj", "--no-build", "--urls", "http://+:8080"]
