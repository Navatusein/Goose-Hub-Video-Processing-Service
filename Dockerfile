#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["VideoProcessingService.csproj", "."]
RUN dotnet restore "./VideoProcessingService.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "VideoProcessingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "VideoProcessingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN apt update
RUN apt install -y ffmpeg libgdiplus
ENTRYPOINT ["dotnet", "VideoProcessingService.dll"]