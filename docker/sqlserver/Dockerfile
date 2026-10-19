# syntax=docker/dockerfile:1
# =============================================================================
# SQL Server para desarrollo (imagen oficial de Microsoft).
# El esquema lo administrará la API con migraciones de EF Core.
# =============================================================================
FROM mcr.microsoft.com/mssql/server:2022-latest

# Licencia aceptada y edición de desarrollo.
ENV ACCEPT_EULA=Y \
    MSSQL_PID=Developer

EXPOSE 1433
