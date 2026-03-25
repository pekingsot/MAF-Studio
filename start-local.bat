@echo off
echo Starting MAF Studio (Local Development)...
echo.

echo ========================================
echo Backend Configuration
echo ========================================
echo Database: 192.168.1.250:5433
echo Username: pekingsot
echo Database: mafstudio
echo ========================================
echo.

echo Starting Backend...
cd backend
start "MAF Studio Backend" cmd /k "dotnet run"
cd ..

echo Backend is starting on http://localhost:5000
echo.

echo Waiting for backend to start...
timeout /t 10 /nobreak >nul

echo Starting Frontend...
cd frontend
start "MAF Studio Frontend" cmd /k "npm start"
cd ..

echo ========================================
echo MAF Studio is now running!
echo ========================================
echo Backend: http://localhost:5000
echo Backend API: http://localhost:5000/api
echo Swagger: http://localhost:5000/swagger
echo Frontend: http://localhost:3000
echo ========================================
echo.
echo To stop services, run: stop-local.bat
echo.