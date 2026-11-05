#!/bin/sh
set -e

CERT_PATH="${ASPNETCORE_Kestrel__Certificates__Default__Path}"
CERT_PASS="${ASPNETCORE_Kestrel__Certificates__Default__Password}"

# Automatically generate a self-signed certificate if HTTPS is enabled and the certificate file does not exist yet.
# If a custom certificate already exists (e.g. mounted at /https/app.pfx), it is preserved and used as-is.
if echo "${ASPNETCORE_URLS}" | grep -qi "https://" && [ -n "${CERT_PATH}" ] && [ ! -f "${CERT_PATH}" ]; then
    echo "HTTPS certificate not found at ${CERT_PATH}. Generating a self-signed certificate..."
    mkdir -p "$(dirname "${CERT_PATH}")" 2>/dev/null || true
    if [ ! -w "$(dirname "${CERT_PATH}")" ]; then
        CERT_PATH="/tmp/aspnetapp.pfx"
        export ASPNETCORE_Kestrel__Certificates__Default__Path="${CERT_PATH}"
    fi
    TMP_KEY="/tmp/aspnetapp.key"
    TMP_CRT="/tmp/aspnetapp.crt"
    openssl req -x509 -newkey rsa:2048 -sha256 -days 3650 -nodes \
        -keyout "${TMP_KEY}" -out "${TMP_CRT}" \
        -subj "/CN=netresvm" \
        -addext "subjectAltName=DNS:localhost,IP:127.0.0.1" 2>/dev/null
    openssl pkcs12 -export -out "${CERT_PATH}" \
        -inkey "${TMP_KEY}" -in "${TMP_CRT}" \
        -passout "pass:${CERT_PASS}"
    rm -f "${TMP_KEY}" "${TMP_CRT}"
    chmod 600 "${CERT_PATH}"
    echo "Self-signed HTTPS certificate generated at ${CERT_PATH}."
fi

exec dotnet NetResVM.Web.dll "$@"
