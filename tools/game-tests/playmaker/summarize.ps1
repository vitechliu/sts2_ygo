param(
    [Parameter(Mandatory = $true)][string[]]$Reports,
    [string]$OutputPath,
    [string]$MarkdownPath
)
$ErrorActionPreference = 'Stop'
$worklist = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'worklist.json') -Encoding UTF8 -Raw | ConvertFrom-Json
$latest = @{}
foreach ($reportPath in $Reports) {
    $resolved = (Resolve-Path -LiteralPath $reportPath).Path
    $report = Get-Content -LiteralPath $resolved -Encoding UTF8 -Raw | ConvertFrom-Json
    foreach ($result in $report.results) {
        $key = '{0}|{1}' -f $result.Name, $result.Upgraded
        if (!$latest.ContainsKey($key) -or [DateTimeOffset]$report.updated -ge $latest[$key].Updated) {
            $latest[$key] = [pscustomobject]@{ Result = $result; Updated = [DateTimeOffset]$report.updated; Report = $resolved; Hash = $report.assemblySha256 }
        }
    }
}
$summary = foreach ($group in ($worklist.rows | Group-Object name | Sort-Object { ($_.Group | Measure-Object row -Minimum).Minimum })) {
    $name = $group.Name
    $base = $latest[('{0}|False' -f $name)]
    $upgrade = $latest[('{0}|True' -f $name)]
    $needsUpgrade = $name -ne '工具衍生物'
    $entries = @($base, $upgrade) | Where-Object { $null -ne $_ }
    $status = if (@($entries | Where-Object {$_.Result.Status -eq '阻塞'}).Count) { '阻塞' }
        elseif (@($entries | Where-Object {$_.Result.Status -eq '失败'}).Count) { '失败' }
        elseif ($base -and $base.Result.Status -eq '通过' -and (!$needsUpgrade -or ($upgrade -and $upgrade.Result.Status -eq '通过'))) { '单机断言通过' }
        else { '未完成' }
    [pscustomobject]@{
        卡名 = $name
        复跑命令 = 'vtest playmaker ' + $group.Group[0].testKey
        表格行 = ($group.Group.row -join ',')
        结论 = $status
        基础版 = if ($base) {$base.Result.Status} else {'未运行'}
        升级版 = if (!$needsUpgrade) {'不适用'} elseif ($upgrade) {$upgrade.Result.Status} else {'未运行'}
        断言数 = (@($entries | ForEach-Object {$_.Result.Checks}).Count)
        原因 = (($entries | ForEach-Object {$_.Result.Error} | Where-Object {$_} | Select-Object -Unique) -join '；')
        报告 = (($entries.Report | Select-Object -Unique) -join ';')
        DLL哈希 = (($entries.Hash | Select-Object -Unique) -join ';')
    }
}
if ($OutputPath) { $summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputPath -Encoding UTF8 }
if ($MarkdownPath) {
    $lines = @('# 藤木游作卡池测试结果', '',
        '本表仅汇总指定报告中每张卡基础/升级的最新单机结算结果，不能替代手动选卡、完整界面或联机验收。表格行号从 1 开始。', '',
        '| 表格行 | 卡名 | 结论 | 基础 / 升级 | 断言数 | 复跑命令 | 原因 |',
        '| --- | --- | --- | --- | --- | --- | --- |')
    foreach ($item in $summary) {
        $reason = $item.原因.Replace('|', '\|').Replace("`r", '').Replace("`n", ' ')
        $lines += '| {0} | {1} | {2} | {3} / {4} | {5} | `{6}` | {7} |' -f $item.表格行, $item.卡名, $item.结论, $item.基础版, $item.升级版, $item.断言数, $item.复跑命令, $reason
    }
    $lines | Set-Content -LiteralPath $MarkdownPath -Encoding UTF8
}
$summary | Format-Table 卡名,结论,基础版,升级版,断言数,原因 -Wrap
if (@($summary | Where-Object {$_.结论 -ne '单机断言通过'}).Count) { exit 1 }
