FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RealmeyeIdentity/RealmeyeIdentity.csproj", "RealmeyeIdentity/"]
RUN dotnet restore "RealmeyeIdentity/RealmeyeIdentity.csproj"
COPY . .
WORKDIR "/src/RealmeyeIdentity"
RUN dotnet build "RealmeyeIdentity.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RealmeyeIdentity.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80801
ENTRYPOINT ["dotnet", "RealmeyeIdentity.dll"]