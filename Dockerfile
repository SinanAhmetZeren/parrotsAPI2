FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ParrotsAPI2.csproj .
RUN dotnet restore ParrotsAPI2.csproj

COPY . .
RUN dotnet publish ParrotsAPI2.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ParrotsAPI2.dll"]
