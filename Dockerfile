FROM mcr.microsoft.com/dotnet/sdk:10.0.302 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Mtpr.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
COPY docs/examples/m5-helpdesk /app/definitions
ENV Mtpr__DefinitionsPath=/app/definitions \
    Mtpr__ConnectionString="Data Source=/data/mtpr.db"
VOLUME /data
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Mtpr.Api.dll"]
