FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AuthenticationModule/AuthenticationModule.csproj", "AuthenticationModule/"]
COPY ["ZPantryModule/ZPantryModule.csproj", "ZPantryModule/"]
COPY ["ZPantry_Backend/ZPantry_Backend.csproj", "ZPantry_Backend/"]

RUN dotnet restore "ZPantry_Backend/ZPantry_Backend.csproj"

COPY . .
RUN dotnet publish "ZPantry_Backend/ZPantry_Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ZPantry_Backend.dll"]
