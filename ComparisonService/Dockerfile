FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ComparisonService.csproj", "."]
RUN dotnet restore "ComparisonService.csproj"
COPY . .
RUN dotnet build "ComparisonService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ComparisonService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "ComparisonService.dll"]