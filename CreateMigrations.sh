#!/bin/sh
if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <migrationname>"
    exit 1
fi
cd API || exit
dotnet ef migrations add $1 --context MangaContext --output-dir Migrations/Manga
dotnet ef migrations add $1 --context LibraryContext --output-dir Migrations/Library
dotnet ef migrations add $1 --context NotificationsContext --output-dir Migrations/Notifications
dotnet ef migrations add $1 --context ActionsContext --output-dir Migrations/Actions