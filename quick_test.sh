#!/bin/bash
cd /workspace/CA_Scanner
echo "Running quick test to verify the fix..."

# Run just one specific test
dotnet test ConditionalAccessExporter.Tests --filter "ComparePoliciesAsync_DifferentPolicies_ShouldReturnDifferentStatus" --logger "console;verbosity=detailed" --no-build || echo "Test failed"

echo "Test run complete"