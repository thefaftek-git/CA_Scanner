#EDIT: Add Husky.NET configuration
module.exports = {
  hooks: {
    'pre-commit': 'dotnet format && dotnet test'
  }
};
