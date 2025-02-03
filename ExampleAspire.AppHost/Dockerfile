# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image for running the application (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8082
EXPOSE 8083

# Build stage: used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy the project file and restore dependencies
COPY ["ExampleAspire.AppHost/ExampleAspire.AppHost.csproj", "ExampleAspire.AppHost/"]
RUN dotnet restore "./ExampleAspire.AppHost/ExampleAspire.AppHost.csproj"

# Copy the entire project and build it
COPY . .
WORKDIR "/src/ExampleAspire.AppHost"
RUN dotnet build "./ExampleAspire.AppHost.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage: prepare the application for deployment
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ExampleAspire.AppHost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage: used for running the application in production
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExampleAspire.AppHost.dll"]
