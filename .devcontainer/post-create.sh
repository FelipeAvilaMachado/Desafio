#!/bin/bash
set -e

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOLUTION_DIR="$REPO_ROOT/src"

echo "📦 Installing dependencies..."

echo "🚀 Installing Aspire CLI..."
dotnet tool update --global aspire.cli >/dev/null 2>&1 || dotnet tool install --global aspire.cli

echo "🐳 Checking Docker availability for Aspire container resources..."
if command -v docker >/dev/null 2>&1; then
  if docker info >/dev/null 2>&1; then
    echo "✅ Docker is available."
  else
    echo "⚠️ Docker CLI is installed but daemon/socket is unavailable."
    echo "   Aspire resources like SQL Server, Redis and RabbitMQ may fail to start."
  fi
else
  echo "⚠️ Docker CLI not found in this devcontainer."
  echo "   Aspire resources like SQL Server, Redis and RabbitMQ may fail to start."
fi

# Clean up stale Windows-built artifacts from the mounted workspace.
# bin/ and obj/ are redirected to /tmp/desafio-build/ on Linux (see Directory.Build.props),
# but Windows-built obj/ folders must be cleared so stale .cs files aren't globbed.
echo "🧹 Clearing Windows build artifacts..."
find "$SOLUTION_DIR" -type d \( -name bin -o -name obj \) -not -path "*/node_modules/*" \
  -exec rm -rf {} + 2>/dev/null || true

# Restore .NET projects
echo "🔧 Restoring .NET projects..."
if [ -f "$SOLUTION_DIR/Desafio.slnx" ]; then
  dotnet restore "$SOLUTION_DIR/Desafio.slnx"
else
  dotnet restore "$SOLUTION_DIR"
fi

# Install frontend dependencies
if [ -d "$REPO_ROOT/frontend" ]; then
  echo "📝 Installing frontend dependencies..."
  cd "$REPO_ROOT/frontend"
  npm install
fi

# Install CDK dependencies if needed
if [ -d "$REPO_ROOT/infra/aws/cdk" ]; then
  echo "☁️  Installing AWS CDK dependencies..."
  cd "$REPO_ROOT/infra/aws/cdk"
  npm install
fi

echo "🧩 Ensuring C# Dev Tools proposed API configuration..."
EXT_PATTERN="/home/vscode/.vscode-server/extensions/jakubkozera.csharp-dev-tools-*"
EXT_DIR="$(ls -d $EXT_PATTERN 2>/dev/null | sort -V | tail -n 1 || true)"
if [ -n "$EXT_DIR" ] && [ -f "$EXT_DIR/package.json" ]; then
  node -e 'const fs=require("fs"); const p=process.argv[1]; const j=JSON.parse(fs.readFileSync(p,"utf8")); const arr=Array.isArray(j.enabledApiProposals)?j.enabledApiProposals:[]; if(!arr.includes("treeViewActiveItem")) arr.push("treeViewActiveItem"); j.enabledApiProposals=arr; fs.writeFileSync(p, JSON.stringify(j,null,2)+"\n"); console.log("✅ Patched", p, "enabledApiProposals=", j.enabledApiProposals.join(", "));' "$EXT_DIR/package.json"
else
  echo "ℹ️  csharp-dev-tools extension not found yet; skipping proposed API patch."
fi

echo "✅ Setup complete!"
echo ""
echo "Tools:"
echo "  aspire --version"
echo ""
echo "Available commands:"
echo "  Backend:"
echo "    dotnet run --project Desafio.Server"
echo "    dotnet run --project Desafio.AppHost"
echo "  Frontend:"
echo "    cd frontend && npm run dev"
echo "  Tests:"
echo "    dotnet test"
