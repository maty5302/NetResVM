#!/bin/bash

# Spuštění SQL Serveru na pozadí
/opt/mssql/bin/sqlservr &

# Čekání na start (SQL Server potřebuje čas na inicializaci)
echo "Čekám na start SQL Serveru..."
sleep 30s

# Spuštění SQL skriptu pro vytvoření tabulek
# Přepínač -C slouží k důvěře certifikátu (nutné pro novější ovladače)
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'SqlDocker2026' -d master -i SQLCreateTablesBc.sql -C

echo "Tabulky byly úspěšně vloženy."

# Udržení kontejneru v chodu (přepnutí procesu na popředí)
wait