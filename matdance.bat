@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
set "MATRUN_DLL=%SCRIPT_DIR%src\Matdance.Cli\bin\Debug\net9.0\Matdance.Cli.dll"
if exist "%MATRUN_DLL%" (
  for %%A in (%*) do (
    if /I "%%~A"=="stop-all" (
      dotnet "%MATRUN_DLL%" %*
      exit /b %ERRORLEVEL%
    )
  )
)
dotnet run --project "%SCRIPT_DIR%src\Matdance.Cli\Matdance.Cli.csproj" -- %*
