# Multi-stage Dockerfile for SurveyBot API
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY SurveyBot.sln ./

# Copy project files
COPY src/SurveyBot.Core/SurveyBot.Core.csproj src/SurveyBot.Core/
COPY src/SurveyBot.Infrastructure/SurveyBot.Infrastructure.csproj src/SurveyBot.Infrastructure/
COPY src/SurveyBot.Bot/SurveyBot.Bot.csproj src/SurveyBot.Bot/
COPY src/SurveyBot.API/SurveyBot.API.csproj src/SurveyBot.API/

# Restore dependencies
RUN dotnet restore src/SurveyBot.API/SurveyBot.API.csproj

# Copy all source code
COPY src/ src/

# Build the project
WORKDIR /src/src/SurveyBot.API
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published files
COPY --from=publish /app/publish .

# Create logs and wwwroot directories for static files
RUN mkdir -p /app/logs && \
    mkdir -p /app/wwwroot/uploads/media

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl --fail http://localhost:8080/health/db || exit 1

# Entry point
ENTRYPOINT ["dotnet", "SurveyBot.API.dll"]
