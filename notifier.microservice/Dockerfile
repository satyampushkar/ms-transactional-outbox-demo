#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["notifier.microservice/notifier.microservice.csproj", "notifier.microservice/"]
COPY ["common.models/common.models.csproj", "common.models/"]
RUN dotnet restore "notifier.microservice/notifier.microservice.csproj"
COPY . .
WORKDIR "/src/notifier.microservice"
RUN dotnet build "notifier.microservice.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "notifier.microservice.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "notifier.microservice.dll"]