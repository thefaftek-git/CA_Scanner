
# Use the official .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Install dependencies
RUN apt-get update && apt-get install -y \
    curl \
    git \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK
RUN curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0

# Set up working directory
WORKDIR /workspace

# Copy project files
COPY . .

# Restore NuGet packages
RUN dotnet restore

# Build the project
RUN dotnet build

# Install global tools
RUN dotnet tool install -g dotnet-format

# Set up pre-commit hooks
RUN husky install

# Expose port for the application
EXPOSE 5000

# Start the application
CMD ["dotnet", "run", "--project", "ConditionalAccessExporter"]

