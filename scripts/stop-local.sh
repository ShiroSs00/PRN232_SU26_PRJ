#!/bin/bash

echo "🛑 Stopping all services..."

# Stop all dotnet processes
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    taskkill //F //IM dotnet.exe 2>/dev/null
else
    pkill -f "dotnet run"
fi

# Stop docker containers
docker-compose down

echo "✅ All services stopped!"
