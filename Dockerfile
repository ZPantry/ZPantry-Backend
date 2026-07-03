FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first so restore can be cached.
COPY AuthenticationModule/AuthenticationModule.csproj AuthenticationModule/
COPY ZPantryModule/ZPantryModule.csproj ZPantryModule/
COPY ZPantry_Backend/ZPantry_Backend.csproj ZPantry_Backend/

RUN dotnet restore ZPantry_Backend/ZPantry_Backend.csproj

# Copy the rest of the backend source.
COPY . .
RUN dotnet publish ZPantry_Backend/ZPantry_Backend.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ZPantry_Backend.dll"]
