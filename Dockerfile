FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

# PDFsharp uses system fonts on Linux; Liberation fonts provide Arial-compatible metrics.
RUN apt-get update \
    && apt-get install -y --no-install-recommends fontconfig fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["AriasEmpireERP.csproj", "."]
RUN dotnet restore "AriasEmpireERP.csproj"
COPY . .
RUN dotnet publish "AriasEmpireERP.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p wwwroot/uploads/contratos wwwroot/uploads/logos \
    && chown -R "$APP_UID:$APP_UID" wwwroot/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "AriasEmpireERP.dll"]
