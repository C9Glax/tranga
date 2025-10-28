if [ $# -ne 1 ]; then
  echo "Provide Migration-name"
  exit 128;
fi

let name=$1

dotnet ef migrations add $name --context MangaContext --output-dir Migrations/Manga
dotnet ef migrations add $name --context LibraryContext --output-dir Migrations/Library
dotnet ef migrations add $name --context NotificationsContext --output-dir Migrations/Notifications
dotnet ef migrations add $name --context ActionsContext --output-dir Migrations/Actions