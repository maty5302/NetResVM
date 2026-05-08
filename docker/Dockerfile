# 1. Fáze: BUILD
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Zkopírujeme hlavní Solution soubor
COPY ["SuperReservationSystem.sln", "./"]

# Zkopírujeme všechny .csproj soubory do jejich příslušných složek
COPY ["SuperReservationSystem/NetResVM.csproj", "SuperReservationSystem/"]
COPY ["BusinessLayer/BusinessLayer.csproj", "BusinessLayer/"]
COPY ["DataLayer/DataLayer.csproj", "DataLayer/"]
COPY ["ApiCisco/ApiCisco.csproj", "ApiCisco/"]
COPY ["ApiEVE/ApiEVE.csproj", "ApiEVE/"]
COPY ["SimpleLogger/SimpleLogger.csproj", "SimpleLogger/"]
COPY ["TelnetConsole/TelnetConsole.csproj", "TelnetConsole/"]
COPY ["tests/NetResVM.UnitTests/NetResVM.UnitTests.csproj", "tests/NetResVM.UnitTests/"]
COPY ["tests/NetResVM.IntegrationTests/NetResVM.IntegrationTests.csproj", "tests/NetResVM.IntegrationTests/"]

# Stáhneme všechny NuGet balíčky pro celou Solution
RUN dotnet restore "SuperReservationSystem.sln"

# Zkopírujeme úplně zbytek kódu (všechny .cs soubory atd.)
COPY . .

# Zkompilujeme hlavní webový projekt (změň cestu, pokud se hlavní projekt jmenuje jinak)
WORKDIR "/src/SuperReservationSystem"
RUN dotnet publish "NetResVM.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Fáze: RUN (Běhové prostředí)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# ENTRYPOINT spouští výslednou zkompilovanou DLL knihovnu webu
ENTRYPOINT ["dotnet", "NetResVM.dll"]
