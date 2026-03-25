@echo off
echo Starting MAF Studio...
echo.

echo Starting PostgreSQL database...
docker-compose up -d postgres

echo Waiting for database to be ready...
timeout /t 10 /nobreak >nul

echo Starting backend service...
docker-compose up -d backend

echo Waiting for backend to be ready...
timeout /t 10 /nobreak >nul

echo Starting frontend service...
docker-compose up -d frontend

echo.
echo ========================================
echo MAF Studio is now running!
echo ========================================
echo Frontend: http://localhost:3000
echo Backend API: http://localhost:5000
echo Swagger: http://localhost:5000/swagger
echo PostgreSQL: localhost:5432
echo ========================================
echo.
echo To view logs, run: docker-compose logs -f
echo To stop services, run: docker-compose down
echo.