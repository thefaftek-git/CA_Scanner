

module.exports = {
  hooks: {
    'pre-commit': 'dotnet format && dotnet test'
  }
};

