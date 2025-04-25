# Requires running PowerShell as Administrator

# Stop on error
$ErrorActionPreference = "Stop"

function Log-Error {
    param($line)
    Write-Error "Error on line: $line"
    exit 1
}

# Trap error
trap {
    Log-Error $_.InvocationInfo.ScriptLineNumber
}

Write-Host "Installing Chocolatey if not installed..."
if (!(Get-Command choco -ErrorAction SilentlyContinue)) {
    Set-ExecutionPolicy Bypass -Scope Process -Force
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))
} else {
    Write-Host "Chocolatey already installed. Skipping."
}

Write-Host "Installing curl..."
choco install curl -y

Write-Host "Installing SQL Server tools (sqlcmd)..."
choco install sqlserver-odbcdriver -y

choco install sqlcmd -y

Write-Host "Installing .NET SDK 8.0..."
choco install dotnet-sdk --version=8.0.100 -y

Write-Host "Installing ASP.NET Core Runtime 8.0..."
choco install dotnet-8.0-aspnetruntime --version=8.0.0 -y

Write-Host "Installing .NET Runtime 8.0..."
choco install dotnet-runtime --version=8.0.0 -y

Write-Host "Installing Microsoft SQL Server 2022..."

# Run post-install SQL script
$SA_PASSWORD = Read-Host -AsSecureString "Create SA password"
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SA_PASSWORD)
$UnsecurePassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

choco install sql-server-express --params "'/SAPWD:$UnsecurePassword /SQLSYSADMINACCOUNTS:BUILTIN\ADMINISTRATORS /FEATURES:SQLEngine /INSTANCENAME:MSSQLSERVER /TCPENABLED:1'" -y

$SqlCmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"

$regPath = "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\MSSQL16.SQLEXPRESS\MSSQLServer"
if (Test-Path $regPath) {
    Set-ItemProperty -Path $regPath -Name LoginMode -Value 2
    Write-Host "LoginMode set to Mixed Mode (2)." -ForegroundColor Green

     # Restart SQL Server service
    Write-Host "Restarting SQL Server service..."
    Restart-Service -Name 'MSSQL$SQLEXPRESS' -Force
    Start-Sleep -Seconds 10 # počkej chvilku, než se znovu spustí
} else {
    Write-Warning "Registry path not found. LoginMode may not have been updated!"
}


# ----------------------------
# ENABLE SA LOGIN + SET PASSWORD
# ----------------------------
Write-Host "Enabling SA login and setting password..." -ForegroundColor Cyan

& $SqlcmdPath -S localhost\SQLEXPRESS -E -Q "IF EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'sa') BEGIN ALTER LOGIN sa ENABLE; ALTER LOGIN sa WITH PASSWORD = '$UnsecurePassword'; END ELSE BEGIN CREATE LOGIN sa WITH PASSWORD = '$UnsecurePassword', CHECK_POLICY = ON; ALTER SERVER ROLE sysadmin ADD MEMBER sa; END"


if (Test-Path "SQLCreateTablesBc.sql") {
    
    & "$SqlCmdPath" -S localhost\SQLEXPRESS -U SA -P $UnsecurePassword -i SQLCreateTablesBc.sql
    Write-Host "Post-install SQL script executed."
} else {
    Write-Warning "SQLCreateTablesBc.sql not found. Skipping post-install SQL script."
}

Write-Host "Installation complete!"
Write-Host "Recommended to check SQLCommandAfterInstall.sql for any additional configuration."
