# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy csproj and restore dependencies
COPY ["storage-labels-api/storage-labels-api.csproj", "storage-labels-api/"]
RUN dotnet restore "storage-labels-api/storage-labels-api.csproj"

# Copy everything else and build
COPY storage-labels-api/ storage-labels-api/
WORKDIR "/src/storage-labels-api"
RUN dotnet build "storage-labels-api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "storage-labels-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variable for ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080

# Start the application
ENTRYPOINT ["dotnet", "storage-labels-api.dll"]
