#!/bin/bash

# wait for database to start...
for i in {60..0}; do
  if /opt/mssql-tools/bin/sqlcmd  -U SA -P $SA_PASSWORD -Q 'SELECT 1;' &> /dev/null; then
    echo "$0: SQL Server started"
    break
  fi
  echo "$0: SQL Server startup in progress..."
  sleep 1
done

echo "$0: Initializing database"
#run the setup script to create the DB and the schema in the DB
/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -d master -Q "create database ${DATABASE_NAME}"
#/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $SA_PASSWORD -d master -i Create_ReplicatedTables.sql

echo "$0: SQL Server Database ready"
