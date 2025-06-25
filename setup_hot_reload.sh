#EDIT: Create script for hot reload setup
#!/bin/bash

# Function to setup hot reload
setup_hot_reload() {
  # Install dotnet-watch tool
  dotnet tool install --global dotnet-watch

  # Add hot reload configuration to the project file
  echo '<DotNetCliToolReference Include="Microsoft.DotNet.Watcher.Tools" Version="2.0.0" />' >> ConditionalAccessExporter/ConditionalAccessExporter.csproj

  echo "Hot reload setup completed successfully."
}

# Setup hot reload
setup_hot_reload
