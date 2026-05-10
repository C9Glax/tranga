FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services.Tasks/Services.Tasks.csproj", "Services.Tasks/"]
COPY ["Common.Services/Common.Services.csproj", "Common.Services/"]
COPY ["Tranga.ServiceDefaults/Tranga.ServiceDefaults.csproj", "Tranga.ServiceDefaults/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["Common.Database/Common.Database.csproj", "Common.Database/"]
COPY ["Extensions/Extensions.csproj", "Extensions/"]
COPY ["NSwagClients/NSwagClients.csproj", "NSwagClients/"]
COPY ["Services.Manga/Services.Manga.csproj", "Services.Manga/"]
RUN dotnet restore "Services.Tasks/Services.Tasks.csproj"
COPY . .
WORKDIR "/src/Services.Tasks"
RUN dotnet build "./Services.Tasks.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Services.Tasks.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Services.Tasks.dll"]
