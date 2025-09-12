@echo off
echo LogTest Scenarios
echo ================
echo.
echo 1. Basic test (BT.Debug.CommandInfo.Commands - Info level)
echo 2. Warning test (BT.Debug.Test - Warn level)  
echo 3. Error test (MyApp.Service.Database - Error level)
echo 4. Interactive mode
echo 5. Help
echo 0. Exit
echo.

:menu
set /p choice="Select scenario (0-5): "

if "%choice%"=="1" (
    echo Running: Basic test...
    dotnet run -- -logger "BT.Debug.CommandInfo.Commands" -message "Test command executed successfully" -level Info
    goto menu
)

if "%choice%"=="2" (
    echo Running: Warning test...
    dotnet run -- -logger "BT.Debug.Test" -message "This is a warning message" -level Warn
    goto menu
)

if "%choice%"=="3" (
    echo Running: Error test...
    dotnet run -- -logger "MyApp.Service.Database" -message "Database connection failed" -level Error -exception "Connection timeout after 30 seconds"
    goto menu
)

if "%choice%"=="4" (
    echo Running: Interactive mode...
    dotnet run
    goto menu
)

if "%choice%"=="5" (
    echo Running: Help...
    dotnet run -- --help
    goto menu
)

if "%choice%"=="0" (
    echo Exiting...
    exit /b 0
)

echo Invalid choice. Please try again.
goto menu
