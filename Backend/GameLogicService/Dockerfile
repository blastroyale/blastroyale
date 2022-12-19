FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

ARG APP_PORT=3000
EXPOSE 3000
ENV ASPNETCORE_URLS=http://+:${APP_PORT}

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GameLogicService/GameLogicService.csproj", "GameLogicService/"]
RUN dotnet restore "GameLogicService/GameLogicService.csproj"
COPY . .
WORKDIR "/src/GameLogicService"
RUN dotnet build "GameLogicService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GameLogicService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GameLogicService.dll"]
