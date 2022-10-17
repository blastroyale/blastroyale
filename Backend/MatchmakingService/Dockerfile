FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app

ARG APP_PORT=3000
EXPOSE 3000

ENV ASPNETCORE_URLS=http://+:${APP_PORT}


FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MatchmakingService/MatchmakingService.csproj", "MatchmakingService/"]
RUN dotnet restore "MatchmakingService/MatchmakingService.csproj"
COPY . .
WORKDIR "/src/MatchmakingService"
RUN dotnet build "MatchmakingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MatchmakingService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN addgroup -S flg -g 2000 && adduser -S flg -u 2000 -G flg
RUN chown -R flg:flg . && \
    chmod -R 750 .
USER flg

ENTRYPOINT ["dotnet", "MatchmakingService.dll"]
