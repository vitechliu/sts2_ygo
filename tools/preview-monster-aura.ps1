$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskConfigPath = Join-Path $taskRoot 'env.props'
if (!(Test-Path -LiteralPath $taskConfigPath)) {
    throw '请先按本机配置补齐项目根目录的 env.props。'
}
[xml]$taskConfig = Get-Content -LiteralPath $taskConfigPath -Raw
$taskGodot = [string]$taskConfig.Project.PropertyGroup.GodotPath
if (!(Test-Path -LiteralPath $taskGodot)) {
    throw 'env.props 中的 GodotPath 不存在，请指向本机 Godot 4.5.1 .NET 编辑器。'
}
Push-Location $taskRoot
try {
    dotnet build VYgo.csproj -c Debug
    if ($LASTEXITCODE -ne 0) { throw '构建失败，未启动预览。' }
    $taskArguments = '--path . --resolution 1120x720 res://VYgo/scenes/demos/monster_aura_demo.tscn'
    # 用户显式运行预览入口，显示 Godot 演示窗口供视觉审核。
    Start-Process -FilePath $taskGodot -ArgumentList $taskArguments -WorkingDirectory $taskRoot
}
finally { Pop-Location }
