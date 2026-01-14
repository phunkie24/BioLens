#!/bin/bash

echo "======================================"
echo "BioLens Build Script"
echo "======================================"
echo ""

# Restore packages
echo "📦 Restoring NuGet packages..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ Package restore failed"
    exit 1
fi

# Build solution
echo ""
echo "🔨 Building solution..."
dotnet build --no-restore

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

# Run tests
echo ""
echo "🧪 Running tests..."
dotnet test --no-build --verbosity normal

if [ $? -ne 0 ]; then
    echo "⚠️  Some tests failed"
else
    echo "✅ All tests passed!"
fi

echo ""
echo "======================================"
echo "Build Complete!"
echo "======================================"
