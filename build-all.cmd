@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

rem ============================================================================
rem  SW2219_ITL_FTS - Build all .csproj (default) or all .sln under this repo
rem
rem  Usage:
rem    build-all.cmd
rem        -> Configuration=Release, build every *.csproj recursively
rem    build-all.cmd Debug
rem        -> Configuration=Debug
rem    build-all.cmd sln
rem        -> Build every *.sln recursively (instead of csproj)
rem    build-all.cmd Debug sln
rem        -> Combine options (order of args does not matter)
rem    build-all.cmd x86
rem        -> Add /p:Platform=x86 (many csproj only define x86)
rem    build-all.cmd Release AnyCPU
rem        -> Add /p:Platform=Any CPU
rem
rem  Requires: Visual Studio MSBuild (vswhere) or MSBuild in a well-known path.
rem  Note: Some projects depend on others' outputs (e.g. bin\common). If the
rem  first pass fails, run this script again; MSBuild will rebuild missing refs
rem  where HintPaths resolve after partial builds.
rem ============================================================================

set "CONFIG=Release"
set "KIND=csproj"
set "PLATFORM="
for %%A in (%*) do (
  if /i "%%~A"=="Debug" set "CONFIG=Debug"
  if /i "%%~A"=="Release" set "CONFIG=Release"
  if /i "%%~A"=="sln" set "KIND=sln"
  if /i "%%~A"=="csproj" set "KIND=csproj"
  if /i "%%~A"=="x86" set "PLATFORM=x86"
  if /i "%%~A"=="x64" set "PLATFORM=x64"
  if /i "%%~A"=="AnyCPU" set "PLATFORM=Any CPU"
)

call :FindMSBuild
if not defined MSBUILD (
  echo [ERROR] MSBuild.exe not found. Install Visual Studio or Build Tools,^
  or add MSBuild to PATH.
  exit /b 1
)

set "PLATFORM_ARG="
if defined PLATFORM set "PLATFORM_ARG=/p:Platform=!PLATFORM!"

echo MSBuild: !MSBUILD!
echo Configuration: !CONFIG!
if defined PLATFORM echo Platform: !PLATFORM!
echo Target: *.!KIND!
echo Root: %CD%
echo.

set /a OK=0
set /a FAIL=0

if /i "!KIND!"=="sln" goto BuildSln
goto BuildCsproj

:BuildCsproj
for /f "delims=" %%F in ('dir /s /b "%CD%\*.csproj" 2^>nul') do (
  echo ----------------------------------------
  echo Building: %%F
  "!MSBUILD!" "%%F" /m /nologo /v:m /t:Build /p:Configuration=!CONFIG! !PLATFORM_ARG!
  if errorlevel 1 (
    echo [FAIL] %%F
    set /a FAIL+=1
  ) else (
    set /a OK+=1
  )
)
goto Summary

:BuildSln
for /f "delims=" %%F in ('dir /s /b "%CD%\*.sln" 2^>nul') do (
  echo ----------------------------------------
  echo Building: %%F
  "!MSBUILD!" "%%F" /m /nologo /v:m /t:Build /p:Configuration=!CONFIG! !PLATFORM_ARG!
  if errorlevel 1 (
    echo [FAIL] %%F
    set /a FAIL+=1
  ) else (
    set /a OK+=1
  )
)
goto Summary

:Summary
echo.
echo ========== Finished ==========
echo Success: !OK!   Failed: !FAIL!
if !FAIL! gtr 0 exit /b 1
exit /b 0

rem ---------------------------------------------------------------------------
rem Locate MSBuild: vswhere (VS 2017+), then common fallbacks.
rem ---------------------------------------------------------------------------
:FindMSBuild
set "MSBUILD="
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "!VSWHERE!" (
  for /f "usebackq delims=" %%I in (`"!VSWHERE!" -latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do (
    set "MSBUILD=%%I"
    goto :FindMSBuildDone
  )
)
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" & goto :FindMSBuildDone
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" & goto :FindMSBuildDone
if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" & goto :FindMSBuildDone
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" & goto :FindMSBuildDone
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" & goto :FindMSBuildDone
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe" & goto :FindMSBuildDone
where msbuild.exe >nul 2>&1 && for /f "delims=" %%I in ('where msbuild.exe 2^>nul') do set "MSBUILD=%%I" & goto :FindMSBuildDone
:FindMSBuildDone
exit /b 0
