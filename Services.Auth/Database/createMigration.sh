#!/bin/sh
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <migrationname>"
    exit 1
fi

dotnet ef migrations add $1 --project ../Common.Database --startup-project ../Services.Auth --context AuthContext --output-dir Auth/Migrations
