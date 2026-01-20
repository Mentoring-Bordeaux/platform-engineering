#!/bin/sh
set -e

# Auth Azure via Managed Identity (ignore si déjà loggé)
az login --identity >/dev/null 2>&1 || true

# (optionnel) choisir la subscription
# az account set --subscription "$ARM_SUBSCRIPTION_ID" >/dev/null 2>&1 || true

exec dotnet api.dll