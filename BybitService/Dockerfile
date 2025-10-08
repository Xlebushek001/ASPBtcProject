FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["BybitService.csproj", "."]
RUN dotnet restore "BybitService.csproj"
COPY . .
RUN dotnet build "BybitService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BybitService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "BybitService.dll"]