FROM mcr.microsoft.com/dotnet/sdk:10.0 AS dev
WORKDIR /src
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
COPY ["ZPantry_Backend/ZPantry_Backend.csproj", "ZPantry_Backend/"]
COPY ["AuthenticationModule/AuthenticationModule.csproj", "AuthenticationModule/"]
COPY ["ZPantryModule/ZPantryModule.csproj", "ZPantryModule/"]
RUN dotnet restore "./ZPantry_Backend/ZPantry_Backend.csproj"
COPY . .
WORKDIR "/src/ZPantry_Backend"
EXPOSE 8080
EXPOSE 8081
CMD ["dotnet", "watch", "run", "--project", "ZPantry_Backend.csproj", "--urls", "http://0.0.0.0:8080"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ZPantry_Backend/ZPantry_Backend.csproj", "ZPantry_Backend/"]
COPY ["AuthenticationModule/AuthenticationModule.csproj", "AuthenticationModule/"]
COPY ["ZPantryModule/ZPantryModule.csproj", "ZPantryModule/"]
RUN dotnet restore "./ZPantry_Backend/ZPantry_Backend.csproj"
COPY . .
WORKDIR "/src/ZPantry_Backend"
RUN dotnet build "./ZPantry_Backend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ZPantry_Backend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ZPantry_Backend.dll"]
