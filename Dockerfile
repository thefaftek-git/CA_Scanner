
# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env

# Set working directory inside the container
WORKDIR /app

# Copy the project file and restore dependencies
COPY ["ConditionalAccessExporter/ConditionalAccessExporter.csproj", "ConditionalAccessExporter/"]
RUN dotnet restore "ConditionalAccessExporter/ConditionalAccessExporter.csproj"

# Copy the rest of the source code
COPY . .

# Build the application
RUN dotnet publish "ConditionalAccessExporter/ConditionalAccessExporter.csproj" -c Release -o out

# Use a smaller runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build-env /app/out .

# Set environment variables for Azure authentication (these will be provided at runtime)
ENV AZURE_TENANT_ID=""
ENV AZURE_CLIENT_ID=""
ENV AZURE_CLIENT_SECRET=""

# Expose port 80 (default for ASP.NET Core)
EXPOSE 80

# Entry point for the application
ENTRYPOINT ["dotnet", "ConditionalAccessExporter.dll"]
