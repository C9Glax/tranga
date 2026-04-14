#!/bin/sh
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <migrationname>"
    exit 1
fi

cd ..
dotnet ef migrations add $1 --project Services.Manga --context MangaContext --output-dir Database/Migrations
