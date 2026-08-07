param(
    [switch]$SkipGitCleanCheck
)

$ErrorActionPreference = 'Stop'

$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

function Assert-File {
    param([Parameter(Mandatory)][string]$RelativePath)

    $Path = Join-Path $Root $RelativePath

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Required file is missing: $RelativePath"
    }

    return $Path
}

function Get-PngInfo {
    param([Parameter(Mandatory)][string]$Path)

    $Bytes = [System.IO.File]::ReadAllBytes($Path)

    if ($Bytes.Length -lt 24) {
        throw "PNG file is too small: $Path"
    }

    $Signature = [byte[]](
        0x89, 0x50, 0x4E, 0x47,
        0x0D, 0x0A, 0x1A, 0x0A
    )

    for ($Index = 0; $Index -lt 8; $Index++) {
        if ($Bytes[$Index] -ne $Signature[$Index]) {
            throw "File is not encoded as PNG: $Path"
        }
    }

    $Width = (
        ([int64]$Bytes[16] -shl 24) -bor
        ([int64]$Bytes[17] -shl 16) -bor
        ([int64]$Bytes[18] -shl 8) -bor
        [int64]$Bytes[19]
    )

    $Height = (
        ([int64]$Bytes[20] -shl 24) -bor
        ([int64]$Bytes[21] -shl 16) -bor
        ([int64]$Bytes[22] -shl 8) -bor
        [int64]$Bytes[23]
    )

    [pscustomobject]@{
        Width  = $Width
        Height = $Height
    }
}

foreach ($Command in @('git')) {
    if (-not (Get-Command $Command -ErrorAction SilentlyContinue)) {
        throw "Required command is unavailable: $Command"
    }
}

if (
    -not $SkipGitCleanCheck -and
    (Test-Path (Join-Path $Root '.git') -PathType Container) -and
    (git status --porcelain)
) {
    throw 'Working tree must be clean before release verification.'
}

$RequiredFiles = @(
    'README.md'
    'LICENSE'
    'THIRD_PARTY_NOTICES.md'
    '.gitignore'
    'SECURITY.md'
    'CHANGELOG.md'
    'Docs/MASTER.md'
    'Docs/CURRENT_BUCKET.md'
    'Docs/REPRODUCTION.md'
    'Docs/release-notes/v0.1.0.md'
    'Screenshots/README.md'
    'Packages/manifest.json'
    'Packages/packages-lock.json'
    'ProjectSettings/ProjectVersion.txt'
    'ProjectSettings/EditorBuildSettings.asset'
)

foreach ($RelativePath in $RequiredFiles) {
    [void](Assert-File -RelativePath $RelativePath)
}

$VersionText = Get-Content `
    -LiteralPath (Join-Path $Root 'ProjectSettings/ProjectVersion.txt') `
    -Raw

if ($VersionText -notmatch '(?m)^m_EditorVersion:\s*2022\.3\.62f1\s*$') {
    throw 'ProjectVersion.txt does not declare Unity 2022.3.62f1.'
}

$ManifestPath = Join-Path $Root 'Packages/manifest.json'
$LockPath = Join-Path $Root 'Packages/packages-lock.json'

try {
    $Manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
}
catch {
    throw "Packages/manifest.json is invalid JSON: $($_.Exception.Message)"
}

try {
    $PackageLock = Get-Content -LiteralPath $LockPath -Raw | ConvertFrom-Json
}
catch {
    throw "Packages/packages-lock.json is invalid JSON: $($_.Exception.Message)"
}

$GltfVersion = $Manifest.dependencies.'com.atteneder.gltfast'

if (-not $GltfVersion) {
    throw 'Manifest does not contain com.atteneder.gltfast.'
}

if ($GltfVersion -match '^(file:|git\+|https?://)' -or $GltfVersion -match '[A-Za-z]:[/\\]') {
    throw "glTFast is not version-pinned through the package registry: $GltfVersion"
}

if ($GltfVersion -notmatch '^\d+\.\d+\.\d+([\-+][0-9A-Za-z\.-]+)?$') {
    throw "glTFast version is not a valid pinned semantic version: $GltfVersion"
}

$OpenUpmRegistry = @($Manifest.scopedRegistries) |
    Where-Object {
        $_.url -eq 'https://package.openupm.com' -and
        'com.atteneder' -in @($_.scopes)
    } |
    Select-Object -First 1

if (-not $OpenUpmRegistry) {
    throw 'Manifest does not contain the required OpenUPM com.atteneder scoped registry.'
}

$LockedGltf = $PackageLock.dependencies.'com.atteneder.gltfast'

if (-not $LockedGltf) {
    throw 'Package lock does not contain com.atteneder.gltfast.'
}

if ($LockedGltf.version -ne $GltfVersion) {
    throw (
        'glTFast manifest and lock versions differ. ' +
        "Manifest: $GltfVersion; Lock: $($LockedGltf.version)"
    )
}

if ($LockedGltf.source -eq 'local') {
    throw 'glTFast remains a local package in packages-lock.json.'
}

$ManifestRaw = Get-Content -LiteralPath $ManifestPath -Raw
$LockRaw = Get-Content -LiteralPath $LockPath -Raw

foreach ($Text in @($ManifestRaw, $LockRaw)) {
    if (
        $Text -match '(?i)file:[A-Za-z]:[/\\]' -or
        $Text -match '(?i)[A-Za-z]:[/\\]Users[/\\]' -or
        $Text -match '(?i)/Users/[^/]+/' -or
        $Text -match '(?i)/home/[^/]+/'
    ) {
        throw 'A machine-specific package path remains in the Unity package files.'
    }
}

$BuildSettingsPath = Join-Path $Root 'ProjectSettings/EditorBuildSettings.asset'
$BuildSettings = Get-Content -LiteralPath $BuildSettingsPath -Raw

$ExpectedScenes = @(
    'Assets/Scenes/BootScene.unity'
    'Assets/Scenes/MainMenuScene.unity'
    'Assets/Scenes/MainScene - Tokyo.unity'
    'Assets/Scenes/GarageScene.unity'
    'Assets/Scenes/RaceScene.unity'
)

foreach ($Scene in $ExpectedScenes) {
    $ScenePath = Join-Path $Root $Scene

    if (-not (Test-Path -LiteralPath $ScenePath -PathType Leaf)) {
        throw "Enabled build scene file is missing: $Scene"
    }

    $EscapedScene = [regex]::Escape($Scene)

    if ($BuildSettings -notmatch "(?s)enabled:\s*1\s*\r?\n\s*path:\s*$EscapedScene") {
        throw "Expected enabled build scene was not found: $Scene"
    }
}

$ScriptRoot = Join-Path $Root 'Assets/Scripts'

if (-not (Test-Path -LiteralPath $ScriptRoot -PathType Container)) {
    throw 'Assets/Scripts is missing.'
}

$CSharpFiles = @(
    Get-ChildItem `
        -LiteralPath $ScriptRoot `
        -Recurse `
        -File `
        -Filter '*.cs'
)

if ($CSharpFiles.Count -lt 20) {
    throw "Too few C# gameplay/tool scripts were found: $($CSharpFiles.Count)"
}

$ExpectedScreenshots = @(
    '01-main-menu.png'
    '02-settings-menu.png'
    '03-garage-vehicle-selection.png'
    '04-vehicle-roster.png'
    '05-race-modes-selected.png'
    '06-race-grid-start.png'
    '07-ai-racing-road.png'
    '08-ai-racing-monster-truck.png'
    '09-freeroam-city-traffic.png'
    '10-mission-marker-race-start.png'
    '11-race-results-screen.png'
)

$ScreenshotRoot = Join-Path $Root 'Screenshots'
$ActualScreenshots = @(
    Get-ChildItem `
        -LiteralPath $ScreenshotRoot `
        -File `
        -Filter '*.png' |
    Select-Object -ExpandProperty Name |
    Sort-Object
)

$MissingScreenshots = @(
    $ExpectedScreenshots |
    Where-Object { $_ -notin $ActualScreenshots }
)

$UnexpectedScreenshots = @(
    $ActualScreenshots |
    Where-Object { $_ -notin $ExpectedScreenshots }
)

if ($MissingScreenshots.Count -gt 0) {
    throw 'Missing screenshots: ' + ($MissingScreenshots -join ', ')
}

if ($UnexpectedScreenshots.Count -gt 0) {
    throw 'Unexpected screenshots: ' + ($UnexpectedScreenshots -join ', ')
}

$Hashes = @{}

foreach ($Name in $ExpectedScreenshots) {
    $Path = Join-Path $ScreenshotRoot $Name
    $Info = Get-PngInfo -Path $Path

    if ($Info.Width -lt 800 -or $Info.Height -lt 450) {
        throw (
            "$Name is smaller than the approved evidence size. " +
            "Actual: $($Info.Width) x $($Info.Height)"
        )
    }

    if ($Info.Width -le $Info.Height) {
        throw "$Name is not a landscape screenshot."
    }

    $Hash = (
        Get-FileHash `
            -LiteralPath $Path `
            -Algorithm SHA256
    ).Hash

    if ($Hashes.ContainsKey($Hash)) {
        throw "Duplicate screenshots detected: $($Hashes[$Hash]) and $Name"
    }

    $Hashes[$Hash] = $Name
}

$TrackedFiles = @(
    git -c core.quotepath=false ls-files
)

$ForbiddenTrackedPrefixes = @(
    'Library/'
    'Temp/'
    'Obj/'
    'Build/'
    'Builds/'
    'Logs/'
    'UserSettings/'
    '.vs/'
    '_Private_DoNotUpload/'
    'DoNotUpload/'
    'artifacts/'
)

foreach ($TrackedFile in $TrackedFiles) {
    $Normalised = $TrackedFile.Replace('\', '/')

    foreach ($Prefix in $ForbiddenTrackedPrefixes) {
        if ($Normalised.StartsWith($Prefix)) {
            throw "Generated or private path is tracked: $TrackedFile"
        }
    }
}

$TextExtensions = @(
    '.cs', '.json', '.md', '.txt', '.yml', '.yaml',
    '.ps1', '.asset', '.meta', '.asmdef', '.xml'
)

$TextFiles = @(
    foreach ($TrackedFile in $TrackedFiles) {
        $FullPath = Join-Path $Root $TrackedFile

        if (-not (Test-Path -LiteralPath $FullPath -PathType Leaf)) {
            continue
        }

        $Extension = [System.IO.Path]::GetExtension($FullPath).ToLowerInvariant()

        if ($Extension -in $TextExtensions) {
            Get-Item -LiteralPath $FullPath -Force
        }
    }
)

$PathPatterns = @(
    '(?i)file:[A-Za-z]:[/\\]'
    '(?i)[A-Za-z]:[/\\]Users[/\\][^/\\]+[/\\]'
    '(?i)/Users/[^/]+/'
    '(?i)/home/[^/]+/'
)

$PathFindings = @(
    foreach ($Pattern in $PathPatterns) {
        $TextFiles |
            Select-String `
                -Pattern $Pattern `
                -ErrorAction SilentlyContinue
    }
)

if ($PathFindings.Count -gt 0) {
    $Summary = $PathFindings |
        ForEach-Object {
            "$($_.Path):$($_.LineNumber)"
        }

    throw (
        "Machine-specific paths were found:`r`n" +
        ($Summary -join "`r`n")
    )
}

$SecretPatterns = @(
    '(?i)client_secret\s*[:=]\s*[^\s#]+'
    '(?i)password\s*[:=]\s*[^\s#]+'
    '(?i)-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----'
)

$SecretFindings = @(
    foreach ($Pattern in $SecretPatterns) {
        $TextFiles |
            Select-String `
                -Pattern $Pattern `
                -ErrorAction SilentlyContinue
    }
)

if ($SecretFindings.Count -gt 0) {
    $Summary = $SecretFindings |
        ForEach-Object {
            "$($_.Path):$($_.LineNumber)"
        }

    throw (
        "Credential-shaped content was found:`r`n" +
        ($Summary -join "`r`n")
    )
}

$ReportRoot = Join-Path $Root 'artifacts/validation'

New-Item `
    -ItemType Directory `
    -Force `
    -Path $ReportRoot |
Out-Null

$ReportPath = Join-Path $ReportRoot 'repository-verification.txt'

$Report = @(
    'Tokyo Nights Racing repository verification'
    "GeneratedUtc: $([DateTimeOffset]::UtcNow.ToString('O'))"
    'UnityEditor: 2022.3.62f1'
    "glTFast: $GltfVersion"
    "EnabledScenes: $($ExpectedScenes.Count)"
    "CSharpFiles: $($CSharpFiles.Count)"
    "Screenshots: $($ExpectedScreenshots.Count)"
    'MachineSpecificPaths: none'
    'CredentialFindings: none'
    'Result: PASS'
)

[System.IO.File]::WriteAllLines(
    $ReportPath,
    $Report,
    (New-Object System.Text.UTF8Encoding($false))
)

Write-Host 'Tokyo Nights Racing repository verification passed.' -ForegroundColor Green
Write-Host "Unity editor: 2022.3.62f1"
Write-Host "glTFast:      $GltfVersion"
Write-Host "C# files:     $($CSharpFiles.Count)"
Write-Host "Screenshots:  $($ExpectedScreenshots.Count)"
Write-Host "Report:       $ReportPath"
