FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app


ARG APP_PORT=7274
EXPOSE 7274

ENV ASPNETCORE_URLS=http://*:${APP_PORT}

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["StandaloneServer/", "StandaloneServer/"]
RUN dotnet restore "StandaloneServer/StandaloneServer.csproj"
COPY . .
WORKDIR "/src/StandaloneServer"
RUN dotnet build "StandaloneServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "StandaloneServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StandaloneServer.dll", "--environment=DEVELOPMENT"]
