#!/bin/sh
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <migrationname>"
    exit 1
fi

cd ..
dotnet ef migrations add $1 --project Services.Tasks --context TasksContext --output-dir Database/Migrations
