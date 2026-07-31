FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore EquipmentRental.slnx --disable-parallel -p:NuGetAudit=false
RUN dotnet publish src/Api/EquipmentRental.Api/EquipmentRental.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM build AS migrations
RUN dotnet tool restore
RUN chmod +x /src/scripts/apply-migrations.sh
ENTRYPOINT ["/src/scripts/apply-migrations.sh"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "EquipmentRental.Api.dll"]
