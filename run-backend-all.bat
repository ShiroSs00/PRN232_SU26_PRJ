@echo off
setlocal

set "ROOT=%~dp0"
set "ASPNETCORE_ENVIRONMENT=Development"

echo ========================================
echo Parking backend - run all services
echo Root: %ROOT%
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo ========================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet command not found. Please install .NET SDK 8+ first.
    pause
    exit /b 1
)

echo Starting services in separate windows...
echo.

start "Auth API :5001" /D "%ROOT%" cmd /k "set ASPNETCORE_ENVIRONMENT=Development&& dotnet run --no-build --project src\Services\Auth\Auth.API\Auth.API.csproj --urls http://localhost:5001"
timeout /t 1 /nobreak >nul

start "Parking API :5002" /D "%ROOT%" cmd /k "set ASPNETCORE_ENVIRONMENT=Development&& dotnet run --no-build --project src\Services\Parking\Parking.API\Parking.API.csproj --urls http://localhost:5002"
timeout /t 1 /nobreak >nul

start "Payment API :5003" /D "%ROOT%" cmd /k "set ASPNETCORE_ENVIRONMENT=Development&& dotnet run --no-build --project src\Services\Payment\Payment.API\Payment.API.csproj --urls http://localhost:5003"
timeout /t 1 /nobreak >nul

start "Report API :5004" /D "%ROOT%" cmd /k "set ASPNETCORE_ENVIRONMENT=Development&& dotnet run --no-build --project src\Services\Report\Report.API\Report.API.csproj --urls http://localhost:5004"
timeout /t 2 /nobreak >nul

start "ApiGateway :5000" /D "%ROOT%" cmd /k "set ASPNETCORE_ENVIRONMENT=Development&& dotnet run --no-build --project src\ApiGateway\ApiGateway.csproj --urls http://localhost:5000"

echo Services are starting:
echo   Auth API     http://localhost:5001/swagger
echo   Parking API  http://localhost:5002/swagger
echo   Payment API  http://localhost:5003/swagger
echo   Report API   http://localhost:5004/swagger
echo   Gateway      http://localhost:5000
echo.
echo Close each service window to stop it.
echo.
pause
