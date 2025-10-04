FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BinanceService.csproj", "."]
RUN dotnet restore "BinanceService.csproj"
COPY . .
RUN dotnet build "BinanceService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BinanceService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Убедитесь, что используется только HTTP
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "BinanceService.dll"]