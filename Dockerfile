# Stage 1: build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy everything
COPY . ./

# Restore dependencies
RUN dotnet restore ./TBD.csproj

RUN dotnet restore ./TestProject/TestProject.csproj

# Build and publish
RUN dotnet publish ./TBD.csproj -c Release -o /out

RUN dotnet publish ./TestProject/TestProject.csproj -c Release -o /out

# Stage 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /out .

# Expose HTTP and HTTPS
EXPOSE 5000
EXPOSE 5001

# Start app
ENTRYPOINT ["dotnet", "TBD.dll"]
