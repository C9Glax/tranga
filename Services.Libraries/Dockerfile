FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services.Libraries/Services.Libraries.csproj", "Services.Libraries/"]
COPY ["Common.Services/Common.Services.csproj", "Common.Services/"]
COPY ["Tranga.ServiceDefaults/Tranga.ServiceDefaults.csproj", "Tranga.ServiceDefaults/"]
COPY ["Common/Common.csproj", "Common/"]
COPY ["Extensions/Extensions.csproj", "Extensions/"]
COPY ["GeneratedExtensionClients/GeneratedExtensionClients.csproj", "GeneratedExtensionClients/"]
COPY ["Common.Database/Common.Database.csproj", "Common.Database/"]
RUN dotnet restore "Services.Libraries/Services.Libraries.csproj"
COPY . .
WORKDIR "/src/Services.Libraries"
RUN dotnet build "./Services.Libraries.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Services.Libraries.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Services.Libraries.dll"]
