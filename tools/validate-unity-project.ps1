param(
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

if (-not $RepoRoot) {
    $RepoRoot = Split-Path -Parent $PSScriptRoot
}

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
Set-Location $RepoRoot

$ProjectVersionPath = Join-Path `
    $RepoRoot `
    'ProjectSettings\ProjectVersion.txt'

if (-not (Test-Path -LiteralPath $ProjectVersionPath -PathType Leaf)) {
    throw "ProjectVersion.txt was not found: $ProjectVersionPath"
}

$VersionText = Get-Content -LiteralPath $ProjectVersionPath -Raw
$Match = [regex]::Match(
    $VersionText,
    '(?m)^m_EditorVersion:\s*(?<version>[^\s]+)\s*$'
)

if (-not $Match.Success) {
    throw 'Could not read the Unity editor version.'
}

$EditorVersion = $Match.Groups['version'].Value

if ($EditorVersion -ne '2022.3.62f1') {
    throw "Expected Unity 2022.3.62f1, found $EditorVersion."
}

$UnityCandidates = @(
    "C:\Program Files\Unity\Hub\Editor\$EditorVersion\Editor\Unity.exe"
    "C:\Program Files\Unity\Editor\Unity.exe"
    "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor\$EditorVersion\Editor\Unity.exe"
)

$UnityExe = $UnityCandidates |
    Where-Object {
        Test-Path -LiteralPath $_ -PathType Leaf
    } |
    Select-Object -First 1

if (-not $UnityExe) {
    $HubRoot = 'C:\Program Files\Unity\Hub\Editor'

    if (Test-Path -LiteralPath $HubRoot -PathType Container) {
        $UnityExe = Get-ChildItem `
            -LiteralPath $HubRoot `
            -Directory `
            -ErrorAction SilentlyContinue |
        Where-Object {
            $_.Name -eq $EditorVersion
        } |
        ForEach-Object {
            Join-Path $_.FullName 'Editor\Unity.exe'
        } |
        Where-Object {
            Test-Path -LiteralPath $_ -PathType Leaf
        } |
        Select-Object -First 1
    }
}

if (-not $UnityExe) {
    throw @"
Unity $EditorVersion was not found.

Install the exact editor through Unity Hub, then rerun this validator.
"@
}

$RunningProjectEditors = @(
    Get-CimInstance `
        Win32_Process `
        -Filter "Name = 'Unity.exe'" `
        -ErrorAction SilentlyContinue |
    Where-Object {
        $_.CommandLine -and
        $_.CommandLine.IndexOf(
            $RepoRoot,
            [System.StringComparison]::OrdinalIgnoreCase
        ) -ge 0
    }
)

if ($RunningProjectEditors.Count -gt 0) {
    throw @"
Tokyo Nights Racing is currently open in Unity.

Close the Unity Editor for this project, then rerun validation.
"@
}

$LogRoot = Join-Path $RepoRoot 'artifacts\validation'

New-Item `
    -ItemType Directory `
    -Force `
    -Path $LogRoot |
Out-Null

$LogPath = Join-Path $LogRoot 'unity-import.log'

if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
    Remove-Item -LiteralPath $LogPath -Force
}

Write-Host 'Running exact-editor Unity import and compile validation...' -ForegroundColor Cyan
Write-Host "Unity:   $UnityExe"
Write-Host "Project: $RepoRoot"
Write-Host "Log:     $LogPath"

# SG1 synchronous Unity process launch
$UnityArgumentString = @(
    '-batchmode'
    '-nographics'
    '-quit'
    '-projectPath'
    ('"' + $RepoRoot + '"')
    '-logFile'
    ('"' + $LogPath + '"')
) -join ' '

$StartInfo = New-Object System.Diagnostics.ProcessStartInfo
$StartInfo.FileName = $UnityExe
$StartInfo.Arguments = $UnityArgumentString
$StartInfo.UseShellExecute = $false
$StartInfo.CreateNoWindow = $true
$StartInfo.WorkingDirectory = Split-Path $UnityExe -Parent

Write-Host 'Starting Unity synchronously...' -ForegroundColor Cyan

$UnityProcess = [System.Diagnostics.Process]::Start($StartInfo)

if (-not $UnityProcess) {
    throw 'Unity process could not be started.'
}

Write-Host "Unity process ID: $($UnityProcess.Id)"
Write-Host 'Waiting for package resolution, import, and compilation...'

$UnityProcess.WaitForExit()
$UnityExitCode = $UnityProcess.ExitCode

Write-Host "Unity process exit code: $UnityExitCode"

# Give Unity a short period to flush and release the custom log file.
for ($Attempt = 1; $Attempt -le 20; $Attempt++) {
    if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
        break
    }

    Start-Sleep -Seconds 1
}

if (-not (Test-Path -LiteralPath $LogPath -PathType Leaf)) {
    $DefaultEditorLog = Join-Path `
        $env:LOCALAPPDATA `
        'Unity\Editor\Editor.log'

    Write-Host ''
    Write-Host 'Unity did not create the requested custom log.' `
        -ForegroundColor Red

    Write-Host "Unity exit code: $UnityExitCode"

    if (Test-Path -LiteralPath $DefaultEditorLog -PathType Leaf) {
        Write-Host ''
        Write-Host 'Tail of the default Unity Editor log:' `
            -ForegroundColor Yellow

        Write-Host $DefaultEditorLog

        Get-Content `
            -LiteralPath $DefaultEditorLog `
            -Tail 100
    }
    else {
        Write-Host ''
        Write-Host 'Default Unity Editor log was also not found:' `
            -ForegroundColor Yellow

        Write-Host $DefaultEditorLog
    }

    throw @"
Unity finished without creating the requested validation log.

Exit code: $UnityExitCode
Expected:  $LogPath
"@
}

$LogText = Get-Content -LiteralPath $LogPath -Raw

$FailurePatterns = @(
    '(?im)\berror\s+CS\d+'
    '(?im)Scripts have compiler errors'
    '(?im)Compilation failed'
    '(?im)Package Manager resolve error'
    '(?im)Failed to resolve packages'
    '(?im)Aborting batchmode due to failure'
    '(?im)Fatal Error'
)

$Failures = @(
    foreach ($Pattern in $FailurePatterns) {
        if ($LogText -match $Pattern) {
            $Matches[0]
        }
    }
)

if ($UnityExitCode -ne 0 -or $Failures.Count -gt 0) {
    throw @"
Unity validation failed.

Exit code: $UnityExitCode
Detected:  $($Failures -join ', ')
Log:       $LogPath
"@
}

$LockPath = Join-Path $RepoRoot 'Packages\packages-lock.json'

if (-not (Test-Path -LiteralPath $LockPath -PathType Leaf)) {
    throw 'Unity did not regenerate Packages/packages-lock.json.'
}

$LockText = Get-Content -LiteralPath $LockPath -Raw

if (
    $LockText -match '(?i)file:[A-Za-z]:[/\\]' -or
    $LockText -match '"source"\s*:\s*"local"'
) {
    throw 'Unity regenerated a machine-specific package lock.'
}

Write-Host 'Unity import and compile validation passed.' -ForegroundColor Green

& (Join-Path $RepoRoot 'tools\verify-repository.ps1') `
    -SkipGitCleanCheck

if ($LASTEXITCODE -ne 0) {
    throw 'Repository verification failed after Unity import.'
}

Write-Host ''
Write-Host 'TOKYO NIGHTS RACING LOCAL VALIDATION PASSED' -ForegroundColor Green
Write-Host "Unity editor: $EditorVersion"
Write-Host "Log:          $LogPath"
