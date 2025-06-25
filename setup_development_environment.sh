#EDIT: Create script to set up the development environment
#!/bin/bash

# Install dependencies
./install_dependencies.sh

# Set up pre-commit hooks
husky install
