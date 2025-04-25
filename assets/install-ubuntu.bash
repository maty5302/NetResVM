#!/bin/bash

set -e

log_error() {
  echo "Error on line: $1"
  exit 1
}
trap 'log_error $LINENO' ERR

echo "Installing curl..."
sudo apt install curl

echo "Installing libldap2.5-0 library dependency..."
#for LDAP connection Ubuntu 24.04 and newer also .NET 8.0 and app dependency
sudo dpkg -i libldap-2.5-0_amd64.deb

echo "importing the Microsoft repository GPG keys"
if ! sudo apt-key list 2>/dev/null | grep -q "Microsoft (Release signing)"; then
    curl https://packages.microsoft.com/keys/microsoft.asc | sudo tee /etc/apt/trusted.gpg.d/microsoft.asc > /dev/null
    echo "Microsoft GPG key added."
else
    echo "Microsoft GPG key already exists. Skipping."
fi

echo "importing the Microsoft SQL Server 2022 Ubuntu repository"
if ! grep -q "packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list" /etc/apt/sources.list.d/*; then
    curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list > /dev/null
    echo "Microsoft SQL Server repository added."
else
    echo "Microsoft SQL Server repository already exists. Skipping."
fi

echo "Installing mssql-tools and dependencies..."
curl -sSL https://packages.microsoft.com/config/ubuntu/22.04/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list > /dev/null



echo "installing the Microsoft SQL Server 2022 and Microsoft SQL tools"
sudo apt-get update
sudo ACCEPT_EULA=Y apt install -y mssql-tools unixodbc-dev
sudo apt-get install -y mssql-server



echo "Add mssql-tools to PATH if not already present"
if ! grep -q '/opt/mssql-tools/bin' ~/.bashrc; then
    echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
    source ~/.bashrc
    echo "Added mssql-tools to PATH."
else
    echo "mssql-tools already in PATH. Skipping."
fi


echo "configuring the Microsoft SQL Server 2022"
sudo /opt/mssql/bin/mssql-conf setup

#Running post-install SQL script..."
echo "Running post-install SQL script..."

read -s -p "Enter SA password: " SA_PASSWORD
echo

/opt/mssql-tools/bin/sqlcmd -S localhost -U SA -P "$SA_PASSWORD" -i SQLCreateTablesBc.sql
echo "Post-install SQL script executed."


echo "Installing dependencies for .NET 8.0..."
#install dependencies
sudo apt install -y ca-certificates libc6 libgcc-s1 libicu74 liblttng-ust1 libssl3 libstdc++6 libunwind8 zlib1g


echo "Installing .NET 8.0..."
#installing .NET 8.0
sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-8.0

sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-8.0

sudo apt-get install -y dotnet-runtime-8.0

echo "Installation complete!"
echo "Recommended to check SQLCommandAfterInstall.sql for any additional configuration."