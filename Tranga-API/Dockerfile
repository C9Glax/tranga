FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Tranga-API/Tranga-API.csproj", "Tranga-API/"]
RUN dotnet restore "Tranga-API/Tranga-API.csproj"
COPY . .
WORKDIR "/src/Tranga-API"
RUN dotnet build "Tranga-API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tranga-API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tranga-API.dll"]
