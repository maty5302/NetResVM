FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

# Vytvoření pracovní složky
RUN mkdir -p /usr/src/app
WORKDIR /usr/src/app

# Kopírování skriptů do kontejneru
COPY SQLCreateTablesBc.sql /usr/src/app/
COPY entrypoint.sh /usr/src/app/

# Nastavení práv pro spuštění skriptu
RUN chmod +x /usr/src/app/entrypoint.sh

USER mssql

# Spuštění našeho entrypointu
ENTRYPOINT ["/bin/bash", "/usr/src/app/entrypoint.sh"]