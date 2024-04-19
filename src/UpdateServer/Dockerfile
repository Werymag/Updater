#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/UpdateServer/UpdateServer.csproj", "UpdateServer/"]
RUN dotnet restore "UpdateServer/UpdateServer.csproj"
COPY /src .
WORKDIR "/src/UpdateServer"
RUN dotnet build "UpdateServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UpdateServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY ["/src/UpdateServer/Properties", "Properties"]
ENTRYPOINT ["dotnet", "UpdateServer.dll"]