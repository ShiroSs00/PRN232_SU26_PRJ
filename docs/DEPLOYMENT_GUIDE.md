# Deployment Guide - Microservices Architecture

## 🚀 Deployment Options

### Option 1: Local Development (Recommended for học tập)
### Option 2: Docker Compose (Recommended for demo)
### Option 3: Cloud Deployment (Production)

---

## 🏠 Option 1: Local Development

### Prerequisites

```bash
# Check .NET SDK
dotnet --version  # Should be 8.0+

# Check MongoDB
mongosh --version

# Check ports available
netstat -ano | findstr "5000 5001 5002 5003 5004 27017"
```

### Step 1: Start MongoDB

```bash
# Windows - Start MongoDB service
net start MongoDB

# Or run MongoDB in Docker
docker run -d -p 27017:27017 --name mongodb mongo:latest
```

### Step 2: Start Services

**Terminal 1 - Auth Service:**
```bash
cd src/Services/Auth/Auth.API
dotnet run --urls="http://localhost:5001"
```

**Terminal 2 - Parking Service:**
```bash
cd src/Services/Parking/Parking.API
dotnet run --urls="http://localhost:5002"
```

**Terminal 3 - Payment Service:**
```bash
cd src/Services/Payment/Payment.API
dotnet run --urls="http://localhost:5003"
```

**Terminal 4 - Report Service:**
```bash
cd src/Services/Report/Report.API
dotnet run --urls="http://localhost:5004"
```

**Terminal 5 - API Gateway:**
```bash
cd src/ApiGateway
dotnet run --urls="http://localhost:5000"
```

**Terminal 6 - Frontend:**
```bash
cd frontend
npm run dev
```

### Step 3: Verify Services

```bash
# Check Auth Service
curl http://localhost:5001/health

# Check Parking Service
curl http://localhost:5002/health

# Check Payment Service
curl http://localhost:5003/health

# Check Report Service
curl http://localhost:5004/health

# Check Gateway
curl http://localhost:5000/health
```

---

## 🐳 Option 2: Docker Compose (Recommended)

### Prerequisites

```bash
# Check Docker
docker --version
docker-compose --version
```

### Step 1: Build và Run

```bash
# Build all services
docker-compose build

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Step 2: Check containers

```bash
# List running containers
docker ps

# Expected output:
# CONTAINER ID   IMAGE                    PORT                  STATUS
# xxx            parking-gateway          0.0.0.0:5000->80      Up
# xxx            parking-auth-service     0.0.0.0:5001->80      Up
# xxx            parking-parking-service  0.0.0.0:5002->80      Up
# xxx            parking-payment-service  0.0.0.0:5003->80      Up
# xxx            parking-report-service   0.0.0.0:5004->80      Up
# xxx            mongo                    0.0.0.0:27017->27017  Up
```

### Step 3: Verify Services

```bash
# Test qua Gateway
curl http://localhost:5000/health

# Access Swagger UIs
# Gateway: http://localhost:5000/swagger
# Auth: http://localhost:5001/swagger
# Parking: http://localhost:5002/swagger
# Payment: http://localhost:5003/swagger
# Report: http://localhost:5004/swagger
```

### Docker Commands Cheat Sheet

```bash
# Restart một service
docker-compose restart auth-service

# View logs của một service
docker-compose logs -f parking-service

# Exec vào container
docker-compose exec auth-service bash

# Remove all containers
docker-compose down -v

# Rebuild một service
docker-compose build --no-cache auth-service
```

---

## ☁️ Option 3: Cloud Deployment

### 3A. Azure Deployment

#### Prerequisites

```bash
# Install Azure CLI
az --version

# Login
az login

# Set subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"
```

#### Create Resource Group

```bash
az group create \
  --name parking-system-rg \
  --location southeastasia
```

#### Deploy MongoDB (Azure Cosmos DB with MongoDB API)

```bash
az cosmosdb create \
  --name parking-system-db \
  --resource-group parking-system-rg \
  --kind MongoDB \
  --default-consistency-level Session
```

#### Deploy Services as Azure Container Instances

```bash
# Auth Service
az container create \
  --resource-group parking-system-rg \
  --name parking-auth-service \
  --image yourdockerhub/parking-auth-service:latest \
  --dns-name-label parking-auth \
  --ports 80 \
  --environment-variables \
    DatabaseSettings__ConnectionString="YOUR_COSMOS_CONNECTION_STRING" \
    JwtSettings__Secret="YOUR_JWT_SECRET"

# Parking Service
az container create \
  --resource-group parking-system-rg \
  --name parking-parking-service \
  --image yourdockerhub/parking-parking-service:latest \
  --dns-name-label parking-parking \
  --ports 80 \
  --environment-variables \
    DatabaseSettings__ConnectionString="YOUR_COSMOS_CONNECTION_STRING" \
    Services__AuthService="http://parking-auth.southeastasia.azurecontainer.io"

# Payment Service
az container create \
  --resource-group parking-system-rg \
  --name parking-payment-service \
  --image yourdockerhub/parking-payment-service:latest \
  --dns-name-label parking-payment \
  --ports 80

# Report Service
az container create \
  --resource-group parking-system-rg \
  --name parking-report-service \
  --image yourdockerhub/parking-report-service:latest \
  --dns-name-label parking-report \
  --ports 80

# Gateway
az container create \
  --resource-group parking-system-rg \
  --name parking-gateway \
  --image yourdockerhub/parking-gateway:latest \
  --dns-name-label parking-gateway \
  --ports 80
```

#### Access URLs

```
Gateway: http://parking-gateway.southeastasia.azurecontainer.io
Auth: http://parking-auth.southeastasia.azurecontainer.io
Parking: http://parking-parking.southeastasia.azurecontainer.io
Payment: http://parking-payment.southeastasia.azurecontainer.io
Report: http://parking-report.southeastasia.azurecontainer.io
```

---

### 3B. Render Deployment (Đơn giản nhất)

#### Step 1: Push code lên GitHub

```bash
git add .
git commit -m "Microservices architecture ready"
git push origin feature/microservices-architecture
```

#### Step 2: Deploy từng service trên Render.com

1. Login vào https://render.com
2. Tạo **Web Service** mới cho mỗi service:
   - Auth Service
   - Parking Service
   - Payment Service
   - Report Service
   - Gateway

3. Configuration cho mỗi service:

**Auth Service:**
```yaml
Name: parking-auth-service
Runtime: .NET
Build Command: dotnet publish -c Release -o out src/Services/Auth/Auth.API/Auth.API.csproj
Start Command: dotnet out/Auth.API.dll --urls=http://0.0.0.0:$PORT
Environment Variables:
  - DatabaseSettings__ConnectionString: YOUR_MONGODB_URI
  - JwtSettings__Secret: YOUR_JWT_SECRET
  - JwtSettings__Issuer: https://parking-auth-service.onrender.com
  - JwtSettings__Audience: https://parking-gateway.onrender.com
```

**Parking Service:**
```yaml
Name: parking-parking-service
Runtime: .NET
Build Command: dotnet publish -c Release -o out src/Services/Parking/Parking.API/Parking.API.csproj
Start Command: dotnet out/Parking.API.dll --urls=http://0.0.0.0:$PORT
Environment Variables:
  - DatabaseSettings__ConnectionString: YOUR_MONGODB_URI
  - Services__AuthService: https://parking-auth-service.onrender.com
```

**Gateway:**
```yaml
Name: parking-gateway
Runtime: .NET
Build Command: dotnet publish -c Release -o out src/ApiGateway/ApiGateway.csproj
Start Command: dotnet out/ApiGateway.dll --urls=http://0.0.0.0:$PORT
Environment Variables:
  - Routes__AuthService: https://parking-auth-service.onrender.com
  - Routes__ParkingService: https://parking-parking-service.onrender.com
  - Routes__PaymentService: https://parking-payment-service.onrender.com
  - Routes__ReportService: https://parking-report-service.onrender.com
```

#### Step 3: Deploy MongoDB

**Option A: MongoDB Atlas (Free tier 512MB)**
1. Tạo cluster tại https://www.mongodb.com/cloud/atlas
2. Get connection string
3. Update tất cả services với connection string mới

**Option B: Render MongoDB**
```yaml
Name: parking-mongodb
Type: Private Service
Docker Image: mongo:latest
Disk: 1GB (free tier)
```

---

## 🔐 Environment Variables Management

### Development (.env.local)

```env
# Database
DATABASE_CONNECTION_STRING=mongodb://localhost:27017
DATABASE_NAME=ParkingSystemDB

# JWT
JWT_SECRET=your-super-secret-jwt-key-min-32-characters
JWT_ISSUER=http://localhost:5000
JWT_AUDIENCE=http://localhost:5000
JWT_EXPIRES_IN_MINUTES=60

# Service URLs
AUTH_SERVICE_URL=http://localhost:5001
PARKING_SERVICE_URL=http://localhost:5002
PAYMENT_SERVICE_URL=http://localhost:5003
REPORT_SERVICE_URL=http://localhost:5004

# CORS
ALLOWED_ORIGINS=http://localhost:5173
```

### Production (.env.production)

```env
# Database
DATABASE_CONNECTION_STRING=mongodb+srv://user:pass@cluster.mongodb.net
DATABASE_NAME=ParkingSystemDB

# JWT
JWT_SECRET=${JWT_SECRET_FROM_VAULT}
JWT_ISSUER=https://api.parking-system.com
JWT_AUDIENCE=https://parking-system.com
JWT_EXPIRES_IN_MINUTES=60

# Service URLs
AUTH_SERVICE_URL=https://auth.parking-system.com
PARKING_SERVICE_URL=https://parking.parking-system.com
PAYMENT_SERVICE_URL=https://payment.parking-system.com
REPORT_SERVICE_URL=https://report.parking-system.com

# CORS
ALLOWED_ORIGINS=https://parking-system.com,https://www.parking-system.com
```

---

## 📊 Monitoring & Logging

### Health Checks

Tất cả services phải có health check endpoint:

```csharp
// In every service Program.cs
app.MapHealthChecks("/health");
```

### Centralized Logging với Seq

```bash
# Run Seq container
docker run -d \
  --name seq \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  datalust/seq:latest

# Update all services
builder.Logging.AddSeq("http://localhost:5341");
```

### Application Insights (Azure)

```csharp
// In every service
builder.Services.AddApplicationInsightsTelemetry();
```

```json
// appsettings.json
{
  "ApplicationInsights": {
    "InstrumentationKey": "YOUR_KEY"
  }
}
```

---

## 🔄 CI/CD Pipeline

### GitHub Actions

Tạo `.github/workflows/deploy.yml`:

```yaml
name: Deploy Microservices

on:
  push:
    branches: [main]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
    
    - name: Build Auth Service
      run: dotnet publish -c Release src/Services/Auth/Auth.API/Auth.API.csproj
    
    - name: Build Parking Service
      run: dotnet publish -c Release src/Services/Parking/Parking.API/Parking.API.csproj
    
    - name: Build Payment Service
      run: dotnet publish -c Release src/Services/Payment/Payment.API/Payment.API.csproj
    
    - name: Build Report Service
      run: dotnet publish -c Release src/Services/Report/Report.API/Report.API.csproj
    
    - name: Build Gateway
      run: dotnet publish -c Release src/ApiGateway/ApiGateway.csproj
    
    - name: Login to Docker Hub
      uses: docker/login-action@v2
      with:
        username: ${{ secrets.DOCKER_USERNAME }}
        password: ${{ secrets.DOCKER_PASSWORD }}
    
    - name: Build and push Docker images
      run: |
        docker build -t yourdockerhub/parking-auth:latest -f src/Services/Auth/Auth.API/Dockerfile .
        docker push yourdockerhub/parking-auth:latest
        
        docker build -t yourdockerhub/parking-parking:latest -f src/Services/Parking/Parking.API/Dockerfile .
        docker push yourdockerhub/parking-parking:latest
        
        docker build -t yourdockerhub/parking-payment:latest -f src/Services/Payment/Payment.API/Dockerfile .
        docker push yourdockerhub/parking-payment:latest
        
        docker build -t yourdockerhub/parking-report:latest -f src/Services/Report/Report.API/Dockerfile .
        docker push yourdockerhub/parking-report:latest
        
        docker build -t yourdockerhub/parking-gateway:latest -f src/ApiGateway/Dockerfile .
        docker push yourdockerhub/parking-gateway:latest
```

---

## 🧪 Testing Deployment

### Smoke Tests

```bash
#!/bin/bash
# smoke-test.sh

GATEWAY_URL="http://localhost:5000"

echo "Testing Gateway health..."
curl -f $GATEWAY_URL/health || exit 1

echo "Testing Auth Service (via Gateway)..."
curl -f -X POST $GATEWAY_URL/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Test@123"}' || exit 1

echo "Testing Parking Service (via Gateway)..."
curl -f $GATEWAY_URL/api/v1/parking/buildings || exit 1

echo "All tests passed!"
```

### Load Testing với k6

```javascript
// load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 20 },
    { duration: '1m', target: 50 },
    { duration: '30s', target: 0 },
  ],
};

export default function () {
  // Test Gateway
  let res = http.get('http://localhost:5000/api/v1/parking/buildings');
  check(res, { 'status was 200': (r) => r.status == 200 });
  sleep(1);
}
```

```bash
# Run load test
k6 run load-test.js
```

---

## 🔧 Troubleshooting

### Issue 1: Service không start được

```bash
# Check logs
docker-compose logs auth-service

# Common causes:
# - Port đã được sử dụng
# - MongoDB connection failed
# - Missing environment variables
```

**Solution:**
```bash
# Kill process on port
netstat -ano | findstr :5001
taskkill /PID <PID> /F

# Check MongoDB
mongosh --eval "db.adminCommand('ping')"
```

### Issue 2: Gateway không route được

```bash
# Check Ocelot config
cat src/ApiGateway/ocelot.json

# Test direct service
curl http://localhost:5001/api/v1/auth/health

# Test via gateway
curl http://localhost:5000/api/v1/auth/health
```

**Solution:**
```json
// Verify ocelot.json routes
{
  "DownstreamPathTemplate": "/api/v1/auth/{everything}",
  "DownstreamHostAndPorts": [
    { "Host": "localhost", "Port": 5001 }
  ],
  "UpstreamPathTemplate": "/api/v1/auth/{everything}"
}
```

### Issue 3: JWT validation failed

```bash
# Symptoms: 401 Unauthorized
```

**Solution:**
```csharp
// Ensure all services dùng cùng JWT secret
// Check appsettings.json in each service
{
  "JwtSettings": {
    "Secret": "SAME_SECRET_ACROSS_ALL_SERVICES",
    "Issuer": "SAME_ISSUER",
    "Audience": "SAME_AUDIENCE"
  }
}
```

### Issue 4: MongoDB connection timeout

```bash
# Test connection
mongosh "mongodb://localhost:27017" --eval "db.adminCommand('ping')"
```

**Solution:**
```bash
# Windows: Start MongoDB service
net start MongoDB

# Docker: Run MongoDB
docker run -d -p 27017:27017 mongo:latest

# Check firewall
netsh advfirewall firewall add rule name="MongoDB" dir=in action=allow protocol=TCP localport=27017
```

---

## 📚 Quick Reference

### Service Ports

| Service | Port | URL |
|---------|------|-----|
| Gateway | 5000 | http://localhost:5000 |
| Auth | 5001 | http://localhost:5001 |
| Parking | 5002 | http://localhost:5002 |
| Payment | 5003 | http://localhost:5003 |
| Report | 5004 | http://localhost:5004 |
| MongoDB | 27017 | mongodb://localhost:27017 |
| Frontend | 5173 | http://localhost:5173 |

### Useful Commands

```bash
# Start all services (Docker Compose)
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f

# Restart a service
docker-compose restart auth-service

# Rebuild a service
docker-compose build --no-cache auth-service

# Clean up everything
docker-compose down -v
docker system prune -a
```

---

**Deployment complete! 🎉**
