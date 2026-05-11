# 1. BUILD
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NetResVM.sln", "./"]

COPY ["NetResVM.Web/NetResVM.Web.csproj", "NetResVM.Web/"]
COPY ["BusinessLayer/BusinessLayer.csproj", "BusinessLayer/"]
COPY ["DataLayer/DataLayer.csproj", "DataLayer/"]
COPY ["ApiCisco/ApiCisco.csproj", "ApiCisco/"]
COPY ["ApiEVE/ApiEVE.csproj", "ApiEVE/"]
COPY ["SimpleLogger/SimpleLogger.csproj", "SimpleLogger/"]
COPY ["TelnetConsole/TelnetConsole.csproj", "TelnetConsole/"]
COPY ["tests/NetResVM.UnitTests/NetResVM.UnitTests.csproj", "tests/NetResVM.UnitTests/"]
COPY ["tests/NetResVM.IntegrationTests/NetResVM.IntegrationTests.csproj", "tests/NetResVM.IntegrationTests/"]

RUN dotnet restore "NetResVM.sln"

COPY . .

WORKDIR "/src/NetResVM.Web"
RUN dotnet publish "NetResVM.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2.RUN 
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

RUN apt-get update && \
    apt-get install -y libldap-common libldap2-dev && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "NetResVM.Web.dll"]
