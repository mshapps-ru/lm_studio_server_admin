@echo off
chcp 1251 >nul
setlocal

taskkill /F /IM LmStudioServerAdmin.exe 
d:
cd D:\WorkHome\T3Code\lm_studio_server_admin
dotnet build

set SERVICE_NAME=LmStudioServerAdmin
set DEST_DIR=D:\WorkHome\AppForDevelop\LmStudioServerAdmin
set SOURCE_DIR=D:\WorkHome\T3Code\lm_studio_server_admin\bin\Debug\net10.0

echo ========================================
echo Остановка службы %SERVICE_NAME%...
echo ========================================
net stop %SERVICE_NAME% 2^>nul
if %errorlevel% neq 0 (
    echo Служба уже остановлена или не найдена.
)

echo.
echo ========================================
echo Очистка целевой директории...
echo ========================================
for /F "delims=" %%F in ('dir "%DEST_DIR%\*" /B /A-D 2^>nul') do (
    if /I not "%%F"=="config.json" (
        if /I not "%%F"=="app.log" (
            del /Q "%DEST_DIR%\%%F"
        )
    )
)
for /D /R "%DEST_DIR%" %%D in (*) do (
    if /I not "%%D"=="%DEST_DIR%" (
        rmdir /S /Q "%%D"
    )
)

echo.
echo ========================================
echo Копирование файлов из источника...
echo ========================================
robocopy "%SOURCE_DIR%" "%DEST_DIR%" /E /NFL /NDL /NJH /NJS

if %errorlevel% leq 1 (
    echo Копирование завершено успешно.
) else (
    echo WARNING: При копировании возникли проблемы.
)

echo.
echo ========================================
echo Запуск службы %SERVICE_NAME%...
echo ========================================
net start %SERVICE_NAME%
if %errorlevel% neq 0 (
    echo ERROR: Не удалось запустить службу.
    exit /B 1
)

echo.
echo ========================================
echo Готово! Служба запущена.
echo ========================================

endlocal