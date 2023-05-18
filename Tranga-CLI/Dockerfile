FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Tranga-CLI/Tranga-CLI.csproj", "Tranga-CLI/"]
RUN dotnet restore "Tranga-CLI/Tranga-CLI.csproj"
COPY . .
WORKDIR "/src/Tranga-CLI"
RUN dotnet build "Tranga-CLI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tranga-CLI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tranga-CLI.dll"]
