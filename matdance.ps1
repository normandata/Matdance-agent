$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$configuration = if ([string]::IsNullOrWhiteSpace($env:MATDANCE_SOURCE_CONFIGURATION)) { "Debug" } else { $env:MATDANCE_SOURCE_CONFIGURATION }
$dll = Join-Path $dir "src\Matdance.Cli\bin\$configuration\net9.0\Matdance.Cli.dll"

function Get-MatdanceCommandParts {
    param([string[]]$Arguments)
    $parts = @()
    for ($i = 0; $i -lt $Arguments.Count; $i++) {
        $arg = $Arguments[$i]
        if ($arg -eq "--agents-dir") {
            $i++
            continue
        }
        if ($arg.StartsWith("--agents-dir=")) {
            continue
        }
        if ($arg.StartsWith("-")) {
            continue
        }
        $parts += $arg
    }
    return $parts
}

function Test-MatdanceDirectDllCommand {
    param([string[]]$Parts)
    if ($Parts.Count -eq 0) { return $false }
    if ($Parts[0] -eq "stop-all") { return $true }
    if ($Parts[0] -ne "web-ui" -or $Parts.Count -lt 2) { return $false }

    if ($Parts[1] -in @("stop", "stop-all", "status", "supervise")) {
        return $true
    }
    if ($Parts[1] -eq "supervisor" -and $Parts.Count -ge 3 -and $Parts[2] -in @("status", "disable")) {
        return $true
    }
    return $false
}

function Test-MatdanceRestoreAfterSuccess {
    param([string[]]$Parts)
    if ($Parts.Count -eq 0) { return $true }
    if ($Parts[0] -eq "stop-all") { return $false }
    if ($Parts[0] -ne "web-ui" -or $Parts.Count -lt 2) { return $true }

    if ($Parts[1] -in @("start", "restart", "stop", "stop-all", "supervise")) {
        return $false
    }
    if ($Parts[1] -eq "supervisor" -and $Parts.Count -ge 3 -and $Parts[2] -in @("enable", "disable")) {
        return $false
    }
    return $true
}

function Get-MatdanceRunningWebUi {
    if (!(Test-Path $dll)) { return $null }
    $statusText = (& dotnet $dll web-ui status 2>$null | Out-String)
    if ($statusText -match "running at http://(?<host>\[[^\]]+\]|[^:\s]+):(?<port>\d+)") {
        [pscustomobject]@{
            Host = $Matches["host"].Trim("[", "]")
            Port = [int]$Matches["port"]
        }
    }
}

$commandParts = @(Get-MatdanceCommandParts -Arguments $args)

if ((Test-Path $dll) -and (Test-MatdanceDirectDllCommand -Parts $commandParts)) {
    & dotnet $dll @args
    exit $LASTEXITCODE
}

$restoreAfterSuccess = Test-MatdanceRestoreAfterSuccess -Parts $commandParts
$previousWebUi = Get-MatdanceRunningWebUi
if ($previousWebUi -ne $null) {
    & dotnet $dll web-ui stop *> $null
}

$matdanceExit = 1
try {
    & dotnet run -c $configuration --project "$dir\src\Matdance.Cli\Matdance.Cli.csproj" -- @args
    $matdanceExit = if ($global:LASTEXITCODE -ne $null) { $global:LASTEXITCODE } else { 0 }
}
finally {
    if ($previousWebUi -ne $null -and ($matdanceExit -ne 0 -or $restoreAfterSuccess)) {
        & dotnet $dll web-ui start --mode preserve --host $previousWebUi.Host --port $previousWebUi.Port *> $null
    }
}

exit $matdanceExit
