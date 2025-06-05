# 1. .NET çalışma zamanı (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

# 2. SDK ile build ortamı
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "BurnAnalysisApp.csproj"
RUN dotnet publish "BurnAnalysisApp.csproj" -c Release -o /app/publish

# 3. Yayınlanan dosyaları kopyala ve çalıştır
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BurnAnalysisApp.dll"]
