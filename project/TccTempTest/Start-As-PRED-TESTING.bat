@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"

set "EXE=%~dp0TccTempTest.exe"
if not exist "%EXE%" (
  echo 未找到 TccTempTest.exe
  pause
  exit /b 1
)

REM 优先本机账号；若为域账号请改成 DOMAIN\PRED-TESTING
set "TARGET=%COMPUTERNAME%\PRED-TESTING"
if not "%USERDOMAIN%"=="" if /I not "%USERDOMAIN%"=="%COMPUTERNAME%" (
  set "TARGET=%USERDOMAIN%\PRED-TESTING"
)

echo 将以账号 %TARGET% 启动 TccTempTest（runas 会提示输入密码）
echo.
runas /user:%TARGET% "%EXE%"
if errorlevel 1 (
  echo.
  echo runas 失败。可先注销并以 PRED-TESTING 登录后再直接运行 TccTempTest.exe
  pause
)
endlocal
