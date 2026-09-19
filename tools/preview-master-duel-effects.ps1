param([switch]$Verify)
# 本文件使用带 BOM 的 UTF-8，兼容 Windows PowerShell 5.1 的脚本读取方式。
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
[xml]$taskConfig = Get-Content -LiteralPath (Join-Path $taskRoot 'env.props') -Raw -Encoding UTF8
$taskGodot = [string]$taskConfig.Project.PropertyGroup.GodotPath
if (!(Test-Path -LiteralPath $taskGodot)) { throw 'env.props 中的 GodotPath 不存在。' }
Push-Location $taskRoot
try {
    Write-Host '正在构建独立特效预览……'
    dotnet build VYgo.csproj -c Debug -p:SkipModInstall=true
    if ($LASTEXITCODE -ne 0) { throw '构建失败，未启动预览。' }
    $taskLogDirectory = Join-Path $taskRoot '.context/master-duel-effects'
    New-Item -ItemType Directory -Path $taskLogDirectory -Force | Out-Null
    $taskLog = Join-Path $taskLogDirectory 'preview.log'
    $taskArguments = @('--path', '.', '--resolution', '1280x800', '--rendering-method', 'mobile',
        '--log-file', '.context/master-duel-effects/preview.log', 'res://VYgo/scenes/demos/master_duel_effect_browser.tscn')
    if ($Verify) { $taskArguments += @('--', '--md-effects-verify') }
    $taskConsole = Join-Path (Split-Path -Parent $taskGodot) ([IO.Path]::GetFileNameWithoutExtension($taskGodot) + '_console.exe')
    Write-Host '正在导入预览资源……'
    $taskImportArguments = @('--headless', '--editor', '--path', '.', '--import', '--log-file', '.context/master-duel-effects/import.log')
    if (Test-Path -LiteralPath $taskConsole) {
        & $taskConsole @taskImportArguments
        if ($LASTEXITCODE -ne 0) { throw 'Godot 资源导入失败，详见 .context/master-duel-effects/import.log。' }
    }
    else {
        $taskImportProcess = Start-Process -FilePath $taskGodot -ArgumentList $taskImportArguments -WorkingDirectory $taskRoot -WindowStyle Hidden -Wait -PassThru
        if ($taskImportProcess.ExitCode -ne 0) { throw 'Godot 资源导入失败，详见 .context/master-duel-effects/import.log。' }
    }
    Write-Host "正在打开特效浏览器，运行日志：$taskLog"
    # 优先使用同版本控制台程序，让启动错误留在当前 PowerShell；等待预览关闭后再返回。
    if (Test-Path -LiteralPath $taskConsole) {
        & $taskConsole @taskArguments
        $taskExitCode = $LASTEXITCODE
    }
    else {
        $taskProcess = Start-Process -FilePath $taskGodot -ArgumentList $taskArguments -WorkingDirectory $taskRoot -Wait -PassThru
        $taskExitCode = $taskProcess.ExitCode
    }
    if ($taskExitCode -ne 0) { throw "特效预览异常退出，退出码：$taskExitCode；运行日志：$taskLog" }
    if ($Verify -and (Select-String -LiteralPath $taskLog -Pattern '^(ERROR:|SHADER ERROR:|SCRIPT ERROR:)' -Quiet)) {
        throw "特效运行日志包含错误，验证未通过：$taskLog"
    }
}
finally { Pop-Location }
