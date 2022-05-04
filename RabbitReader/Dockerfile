FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
COPY "P:/dev/pi-hub/pi-hub/pi-hub/csharp/Shared/" .
RUN dotnet restore "RabbitReader.csproj"
COPY . .
WORKDIR "/src/RabbitReader"
RUN dotnet build "RabbitReader.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RabbitReader.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RabbitReader.dll"]