#EDIT: Create script for deployment automation
#!/bin/bash

# Function to deploy the application
deploy_application() {
  # Build the Docker image
  docker build -t ca_scanner:latest .

  # Push the Docker image to a container registry
  docker tag ca_scanner:latest your_registry/ca_scanner:latest
  docker push your_registry/ca_scanner:latest

  # Deploy the application using kubectl
  kubectl apply -f k8s/deployment.yaml

  echo "Application deployed successfully."
}

# Deploy the application
deploy_application
