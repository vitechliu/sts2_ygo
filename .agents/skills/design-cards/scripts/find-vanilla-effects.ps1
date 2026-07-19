[CmdletBinding()]
param(
    [string[]]$Query = @(),
    [string]$ModelId,
    [ValidateSet('All', 'Card', 'Relic')]
    [string]$Kind = 'All',
    [ValidateRange(1, 200)]
    [int]$MaxResults = 30,
    [string]$VanillaRoot,
    [string]$LocalizationRoot,
    [string]$SourceRoot,
    [string]$AgentsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Find-NearestAgentsFile {
    param([Parameter(Mandatory)][string]$StartDirectory)

    $current = Get-Item -LiteralPath $StartDirectory
    if (-not $current.PSIsContainer) {
        $current = $current.Directory
    }

    while ($null -ne $current) {
        $candidate = Join-Path $current.FullName 'AGENTS.md'
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            return $candidate
        }
        $current = $current.Parent
    }

    return $null
}

function Get-VanillaRootFromAgents {
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return $null
    }

    $content = Get-Content -LiteralPath $Path -Raw -Encoding utf8
    $match = [regex]::Match(
        $content,
        '(?m)^\s*STS2_VANILLA_ROOT\s*:\s*(?<root>.+?)\s*$'
    )
    if (-not $match.Success) {
        return $null
    }

    $value = $match.Groups['root'].Value.Trim()
    if ($value.Length -ge 2) {
        $first = $value[0]
        $last = $value[$value.Length - 1]
        if (($first -eq '"' -and $last -eq '"') -or
            ($first -eq "'" -and $last -eq "'") -or
            ($first -eq '`' -and $last -eq '`')) {
            $value = $value.Substring(1, $value.Length - 2)
        }
    }

    if ([System.IO.Path]::IsPathRooted($value)) {
        return $value
    }

    $agentsDirectory = Split-Path -Parent $Path
    return [System.IO.Path]::GetFullPath((Join-Path $agentsDirectory $value))
}

if ($Query.Count -eq 0 -and [string]::IsNullOrWhiteSpace($ModelId)) {
    throw 'Provide at least one -Query regex or an exact -ModelId.'
}

if ([string]::IsNullOrWhiteSpace($VanillaRoot) -and
    -not [string]::IsNullOrWhiteSpace($env:STS2_VANILLA_ROOT)) {
    $VanillaRoot = $env:STS2_VANILLA_ROOT
}

if ([string]::IsNullOrWhiteSpace($VanillaRoot)) {
    if ([string]::IsNullOrWhiteSpace($AgentsPath)) {
        $AgentsPath = Find-NearestAgentsFile -StartDirectory $PSScriptRoot
    }
    if (-not [string]::IsNullOrWhiteSpace($AgentsPath)) {
        $VanillaRoot = Get-VanillaRootFromAgents -Path $AgentsPath
    }
}

if ([string]::IsNullOrWhiteSpace($LocalizationRoot) -and
    -not [string]::IsNullOrWhiteSpace($VanillaRoot)) {
    $LocalizationRoot = Join-Path $VanillaRoot 'localization/zhs'
}

if ([string]::IsNullOrWhiteSpace($SourceRoot) -and
    -not [string]::IsNullOrWhiteSpace($VanillaRoot)) {
    $SourceRoot = Join-Path $VanillaRoot 'src'
}

if ([string]::IsNullOrWhiteSpace($LocalizationRoot) -or
    [string]::IsNullOrWhiteSpace($SourceRoot)) {
    throw 'Vanilla references are not configured. Pass -VanillaRoot, set STS2_VANILLA_ROOT, add "STS2_VANILLA_ROOT: <path>" to the nearest AGENTS.md, or pass both -LocalizationRoot and -SourceRoot.'
}

if (-not (Test-Path -LiteralPath $LocalizationRoot -PathType Container)) {
    throw "Vanilla localization directory not found: $LocalizationRoot"
}

if (-not (Test-Path -LiteralPath $SourceRoot -PathType Container)) {
    throw "Vanilla source directory not found: $SourceRoot"
}

function ConvertTo-ModelId {
    param([Parameter(Mandatory)][string]$Name)

    $withAcronymBoundary = [regex]::Replace($Name, '([A-Z]+)([A-Z][a-z])', '$1_$2')
    $withWordBoundary = [regex]::Replace($withAcronymBoundary, '([a-z0-9])([A-Z])', '$1_$2')
    return $withWordBoundary.ToUpperInvariant()
}

function Get-SourceIndex {
    param([Parameter(Mandatory)][string]$Directory)

    $index = @{}
    if (-not (Test-Path -LiteralPath $Directory -PathType Container)) {
        return $index
    }

    Get-ChildItem -LiteralPath $Directory -Filter '*.cs' -File -Recurse | ForEach-Object {
        $candidateId = ConvertTo-ModelId -Name $_.BaseName
        if (-not $index.ContainsKey($candidateId)) {
            $index[$candidateId] = [System.Collections.Generic.List[string]]::new()
        }
        $index[$candidateId].Add($_.FullName)
    }
    return $index
}

function Get-LocalizedModels {
    param(
        [Parameter(Mandatory)][string]$JsonPath,
        [Parameter(Mandatory)][string]$EntryKind,
        [Parameter(Mandatory)][hashtable]$SourceIndex
    )

    if (-not (Test-Path -LiteralPath $JsonPath -PathType Leaf)) {
        throw "Localization file not found: $JsonPath"
    }

    $json = Get-Content -LiteralPath $JsonPath -Raw -Encoding utf8 | ConvertFrom-Json
    $models = @{}

    foreach ($property in $json.PSObject.Properties) {
        $parts = $property.Name.Split('.', 2)
        if ($parts.Count -ne 2) {
            continue
        }

        $id = $parts[0].ToUpperInvariant()
        if (-not $models.ContainsKey($id)) {
            $models[$id] = @{}
        }
        $models[$id][$parts[1]] = [string]$property.Value
    }

    foreach ($id in ($models.Keys | Sort-Object)) {
        if (-not [string]::IsNullOrWhiteSpace($ModelId) -and $id -ne $ModelId.ToUpperInvariant()) {
            continue
        }

        $fields = $models[$id]
        $searchText = (($fields.GetEnumerator() |
                Where-Object { $_.Key -eq 'title' -or $_.Key -match 'description$' } |
                Sort-Object Key |
                ForEach-Object { $_.Value }) -join "`n")
        $matched = $true
        foreach ($pattern in $Query) {
            if ($searchText -notmatch $pattern -and $id -notmatch $pattern) {
                $matched = $false
                break
            }
        }
        if (-not $matched) {
            continue
        }

        $sources = if ($SourceIndex.ContainsKey($id)) {
            $SourceIndex[$id] -join '; '
        } else {
            '(unresolved; search source filenames manually)'
        }

        [pscustomobject]@{
            Kind = $EntryKind
            ModelId = $id
            Title = $fields['title']
            Description = $fields['description']
            Source = $sources
        }
    }
}

$specs = @()
if ($Kind -in @('All', 'Card')) {
    $specs += [pscustomobject]@{
        Kind = 'Card'
        Json = Join-Path $LocalizationRoot 'cards.json'
        Source = Join-Path $SourceRoot 'Core\Models\Cards'
    }
}
if ($Kind -in @('All', 'Relic')) {
    $specs += [pscustomobject]@{
        Kind = 'Relic'
        Json = Join-Path $LocalizationRoot 'relics.json'
        Source = Join-Path $SourceRoot 'Core\Models\Relics'
    }
}

$results = foreach ($spec in $specs) {
    $sourceIndex = Get-SourceIndex -Directory $spec.Source
    Get-LocalizedModels -JsonPath $spec.Json -EntryKind $spec.Kind -SourceIndex $sourceIndex
}

$results |
    Sort-Object Kind, ModelId |
    Select-Object -First $MaxResults |
    Format-List Kind, ModelId, Title, Description, Source
