

module.exports = {
  hooks: {
    'pre-commit': 'dotnet format && dotnet test',
    'pre-push': 'dotnet format'
  }
};

