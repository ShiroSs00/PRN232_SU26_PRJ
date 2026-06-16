#!/bin/bash

# Script để chạy tất cả services locally cho development

echo "🚀 Starting Parking Manager Microservices..."

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# MongoDB dùng Atlas (cloud) nên không cần start database local.
# Nếu muốn dùng MongoDB local qua Docker, bỏ comment dòng dưới:
# docker-compose up -d mongodb

# Start services in separate terminals (Windows)
if [[ "$OSTYPE" == "msys" || "$OSTYPE" == "win32" ]]; then
    echo -e "${GREEN}🔐 Starting Auth Service...${NC}"
    start cmd /k "cd src/Services/AuthService && dotnet run"

    echo -e "${GREEN}🚗 Starting Parking Service...${NC}"
    start cmd /k "cd src/Services/ParkingService && dotnet run"

    echo -e "${GREEN}💳 Starting Payment Service...${NC}"
    start cmd /k "cd src/Services/PaymentService && dotnet run"

    echo -e "${GREEN}📊 Starting Report Service...${NC}"
    start cmd /k "cd src/Services/ReportService && dotnet run"

    echo -e "${BLUE}🌐 Starting API Gateway...${NC}"
    start cmd /k "cd src/ApiGateway && dotnet run"
else
    # For Unix-based systems
    echo -e "${GREEN}🔐 Starting Auth Service...${NC}"
    cd src/Services/AuthService && dotnet run &

    echo -e "${GREEN}🚗 Starting Parking Service...${NC}"
    cd src/Services/ParkingService && dotnet run &

    echo -e "${GREEN}💳 Starting Payment Service...${NC}"
    cd src/Services/PaymentService && dotnet run &

    echo -e "${GREEN}📊 Starting Report Service...${NC}"
    cd src/Services/ReportService && dotnet run &

    echo -e "${BLUE}🌐 Starting API Gateway...${NC}"
    cd src/ApiGateway && dotnet run &
fi

echo ""
echo -e "${GREEN}✅ All services are starting!${NC}"
echo ""
echo "Service URLs:"
echo "  🌐 API Gateway:      http://localhost:5000"
echo "  🔐 Auth Service:     http://localhost:5001"
echo "  🚗 Parking Service:  http://localhost:5002"
echo "  💳 Payment Service:  http://localhost:5003"
echo "  📊 Report Service:   http://localhost:5004"
echo ""
echo "Swagger UIs:"
echo "  Auth:    http://localhost:5001/swagger"
echo "  Parking: http://localhost:5002/swagger"
echo "  Payment: http://localhost:5003/swagger"
echo "  Report:  http://localhost:5004/swagger"
echo ""
