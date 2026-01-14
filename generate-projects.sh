#!/bin/bash

# BioLens Complete Solution Generator
# .NET 10.0 with Agentic AI Design Pattern

set -e

BASE_DIR="/home/claude/BioLens"
cd "$BASE_DIR"

echo "=================================="
echo "BioLens Solution Generator"
echo "Agentic AI Healthcare Assistant"
echo "=================================="

# Function to create project file
create_project_file() {
    local project_name=$1
    local project_type=$2
    local project_path="$BASE_DIR/src/$project_name"
    
    mkdir -p "$project_path"
    
    cat > "$project_path/$project_name.csproj" << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  
  <ItemGroup>
    $([ "$project_type" == "domain" ] && echo '<PackageReference Include="MediatR" Version="12.4.0" />')
    $([ "$project_type" == "application" ] && echo '
    <PackageReference Include="MediatR" Version="12.4.0" />
    <PackageReference Include="AutoMapper" Version="13.0.1" />
    <PackageReference Include="FluentValidation" Version="11.9.0" />')
    $([ "$project_type" == "infrastructure" ] && echo '
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
    <PackageReference Include="LiteDB" Version="5.0.20" />
    <PackageReference Include="Polly" Version="8.4.0" />
    <PackageReference Include="Refit" Version="7.1.2" />
    <PackageReference Include="Microsoft.ML" Version="3.0.1" />')
    $([ "$project_type" == "agents" ] && echo '
    <PackageReference Include="Microsoft.SemanticKernel" Version="1.13.0" />
    <PackageReference Include="Microsoft.SemanticKernel.Agents.Core" Version="1.13.0-alpha" />')
  </ItemGroup>
  
  $([ "$project_type" != "domain" ] && echo '
  <ItemGroup>
    <ProjectReference Include="../BioLens.Domain/BioLens.Domain.csproj" />
  </ItemGroup>')
  
  $([ "$project_type" == "application" ] && echo '
  <ItemGroup>
    <ProjectReference Include="../BioLens.Domain/BioLens.Domain.csproj" />
  </ItemGroup>')
  
  $([ "$project_type" == "infrastructure" ] && echo '
  <ItemGroup>
    <ProjectReference Include="../BioLens.Domain/BioLens.Domain.csproj" />
    <ProjectReference Include="../BioLens.Application/BioLens.Application.csproj" />
  </ItemGroup>')
  
  $([ "$project_type" == "agents" ] && echo '
  <ItemGroup>
    <ProjectReference Include="../BioLens.Domain/BioLens.Domain.csproj" />
    <ProjectReference Include="../BioLens.Application/BioLens.Application.csproj" />
    <ProjectReference Include="../BioLens.Infrastructure/BioLens.Infrastructure.csproj" />
  </ItemGroup>')
</Project>
EOF
}

# Create all project files
echo "Creating project files..."
create_project_file "BioLens.Domain" "domain"
create_project_file "BioLens.Application" "application"
create_project_file "BioLens.Infrastructure" "infrastructure"
create_project_file "BioLens.Agents" "agents"

echo "✓ Project files created"
echo "Solution structure ready for code generation..."
