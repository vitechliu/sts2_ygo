param(
    [string]$FmodStudioCli = "D:\Program Files\FMOD SoundSystem\FMOD Studio 2.03.13\fmodstudiocl.exe"
)

$ErrorActionPreference = "Stop"
$projectDir = $PSScriptRoot
$projectPath = Join-Path $projectDir "STS2.fspro"
$builtBank = Join-Path $projectDir "Build\desktop\VYgo.bank"
$builtGuids = Join-Path $projectDir "Build\GUIDs.txt"
$modBankDir = Join-Path $projectDir "..\VYgo\banks"
$eventPath = "event:/vygo/music/main_menu"

if (-not (Test-Path -LiteralPath $FmodStudioCli -PathType Leaf)) {
    throw "FMOD Studio CLI not found: $FmodStudioCli"
}

& $FmodStudioCli -build -banks VYgo -platforms Desktop -export-guids $projectPath
if ($LASTEXITCODE -ne 0) {
    throw "FMOD build failed with exit code $LASTEXITCODE"
}

if (-not (Test-Path -LiteralPath $builtBank -PathType Leaf)) {
    throw "VYgo.bank was not generated: $builtBank"
}
if (-not (Test-Path -LiteralPath $builtGuids -PathType Leaf)) {
    throw "GUIDs.txt was not generated: $builtGuids"
}
if (-not (Select-String -LiteralPath $builtGuids -SimpleMatch $eventPath -Quiet)) {
    throw "GUIDs.txt does not contain event: $eventPath"
}

New-Item -ItemType Directory -Path $modBankDir -Force | Out-Null
Copy-Item -LiteralPath $builtBank -Destination (Join-Path $modBankDir "VYgo.bank") -Force
Copy-Item -LiteralPath $builtGuids -Destination (Join-Path $modBankDir "VYgo.guids.txt") -Force

Write-Host "VYgo audio built and synchronized to VYgo/banks."
