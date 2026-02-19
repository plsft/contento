# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config (Noundry private feed), build props, and project files for restore
COPY nuget.config Directory.Build.props ./
COPY src/Contento.Web/*.csproj src/Contento.Web/
COPY src/Contento.Core/*.csproj src/Contento.Core/
COPY src/Contento.Services/*.csproj src/Contento.Services/
COPY src/Contento.Plugins/*.csproj src/Contento.Plugins/
RUN dotnet restore src/Contento.Web/Contento.Web.csproj

# Copy everything else and publish
COPY . .
RUN dotnet publish src/Contento.Web/Contento.Web.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Create writable directories for uploads and Tailbreeze CSS compilation
RUN mkdir -p /app/wwwroot/uploads /app/Tailbreeze \
    && chown -R app:app /app/wwwroot/uploads /app/Tailbreeze

USER app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Contento.Web.dll"]
