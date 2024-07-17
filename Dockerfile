#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["GateWay.API.csproj", "GateWay.API/"]
RUN dotnet restore "GateWay.API/GateWay.API.csproj"

WORKDIR "/src/GateWay.API"
COPY . .

RUN dotnet build "GateWay.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "GateWay.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

RUN chown 777 /root/.aspnet/https/

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GateWay.API.dll"]