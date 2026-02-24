FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PDF_Downloader/PDF_Downloader.csproj", "PDF_Downloader/"]
RUN dotnet restore "PDF_Downloader/PDF_Downloader.csproj"
COPY . .
WORKDIR "/src/PDF_Downloader"
RUN dotnet build "./PDF_Downloader.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PDF_Downloader.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PDF_Downloader.dll"]
