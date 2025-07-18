# Use .NET 9.0 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy source code
COPY . .

# Build the application
RUN dotnet build -c Release -o /app/build

# Publish the application
RUN dotnet publish -c Release -o /app/publish

# Use .NET 9.0 runtime for final image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Create logs directory
RUN mkdir -p /app/Logs

# Expose port
EXPOSE 8080

# Set entry point
ENTRYPOINT ["dotnet", "TBD.dll"]
