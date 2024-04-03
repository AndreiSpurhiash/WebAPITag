FROM mcr.microsoft.com/dotnet/aspnet:8.0-nanoserver-1809 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["APITeg/APITeg.csproj", "APITeg/"]
RUN dotnet restore "./APITeg/APITeg.csproj"
COPY . .
WORKDIR "/src/APITeg"
RUN dotnet build "./APITeg.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./APITeg.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "APITeg.dll"]