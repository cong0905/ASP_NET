# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and csproj files for layer caching
COPY ShopNet.sln ./
COPY src/ShopNet.Domain/*.csproj ./src/ShopNet.Domain/
COPY src/ShopNet.Application/*.csproj ./src/ShopNet.Application/
COPY src/ShopNet.Infrastructure/*.csproj ./src/ShopNet.Infrastructure/
COPY src/ShopNet.API/*.csproj ./src/ShopNet.API/
COPY tests/ShopNet.UnitTests/*.csproj ./tests/ShopNet.UnitTests/

# Restore dependencies
RUN dotnet restore ShopNet.sln

# Copy the rest of the source code
COPY . .

# Build and Publish API
WORKDIR /app/src/ShopNet.API
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ShopNet.API.dll"]
