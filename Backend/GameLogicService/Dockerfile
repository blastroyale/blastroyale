FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

ARG APP_PORT=3000
ARG BUILD_COMMIT
ARG BUILD_NUMBER
ARG BUILD_ID

EXPOSE 3000
ENV ASPNETCORE_URLS=http://+:${APP_PORT}
ENV BUILD_COMMIT=$BUILD_COMMIT
ENV BUILD_NUMBER=$BUILD_NUMBER
ENV BUILD_ID=$BUILD_ID

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GameLogicService/GameLogicService.csproj", "GameLogicService/"]
RUN dotnet restore "GameLogicService/GameLogicService.csproj"
COPY . .
WORKDIR "/src/GameLogicService"
RUN dotnet build "GameLogicService.csproj" -c Release -o /app/build

RUN dotnet test "../TestUnit/TestUnit.csproj"

FROM build AS publish
RUN dotnet publish "GameLogicService.csproj" -c Release -o /app/publish

FROM base AS final


WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameLogicService.dll"]
