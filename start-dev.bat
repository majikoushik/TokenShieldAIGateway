@echo off
echo =======================================================
echo Starting TokenShield AI Gateway Local Dev Environment...
echo =======================================================
echo.

:: Start the .NET Backend API in a new terminal window
echo [1/2] Starting Backend API...
start "TokenShield Backend" cmd /k "cd apps\gateway-api\src\TokenShield.Api && title TokenShield Backend && dotnet run"

:: Start the Next.js Frontend in a new terminal window
echo [2/2] Starting Frontend Admin Console...
start "TokenShield Frontend" cmd /k "cd apps\web-admin && title TokenShield Frontend && npm run dev"

echo.
echo Both services are starting in separate windows!
echo - Backend API will be available at: http://localhost:5000
echo - Swagger UI will be available at:  http://localhost:5000/swagger
echo - Frontend Admin Console will be at: http://localhost:3000
echo.
echo Make sure you have PostgreSQL running locally for the backend.
echo.
pause
