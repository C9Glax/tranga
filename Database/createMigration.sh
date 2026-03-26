#!/bin/sh
if [ "$#" -ne 2 ]; then
    echo "Usage: $0 <Context> <migrationname>"
    exit 1
fi

cd ..
dotnet ef migrations add $2 --project Database --context $1 --startup-project API --output-dir $1/Migrations 
