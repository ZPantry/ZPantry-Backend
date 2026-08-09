FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so restore can be cached
COPY AuthenticationModule/AuthenticationModule.csproj AuthenticationModule/
COPY ZPantryModule/ZPantryModule.csproj ZPantryModule/
COPY ZPantry_Backend/ZPantry_Backend.csproj ZPantry_Backend/

# Restore dependencies
RUN dotnet restore ZPantry_Backend/ZPantry_Backend.csproj

# Copy the rest of the backend source
COPY . .

# Publish application
RUN dotnet publish ZPantry_Backend/ZPantry_Backend.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false


# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Render listens on port 8080
ENV ASPNETCORE_URLS=http://+:8080

# Prevent FileSystemWatcher / inotify limit issue on Render
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

EXPOSE 8080

# Required for Kerberos/GSSAPI dependencies
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=build /app/publish .

# Start application
ENTRYPOINT ["dotnet", "ZPantry_Backend.dll"]
