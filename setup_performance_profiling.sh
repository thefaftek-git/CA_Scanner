#EDIT: Create script for performance profiling setup
#!/bin/bash

# Function to setup performance profiling
setup_performance_profiling() {
  # Install dotnet-counters tool
  dotnet tool install --global dotnet-counters

  # Install dotnet-trace tool
  dotnet tool install --global dotnet-trace

  echo "Performance profiling tools setup completed successfully."
}

# Setup performance profiling
setup_performance_profiling
