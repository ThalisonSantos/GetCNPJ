param(
    [string]$PackagePath = "$PSScriptRoot/../../artifacts/H03/packages/GetCNPJ.Community.1.1.0.nupkg",
    [string]$ApprovedSource = "$PSScriptRoot/../../artifacts/H03/approved-source"
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$commit = 'a069e0decc0163f78f9788d204189e4b0d8055e0'
$outputRoot = [IO.Path]::GetFullPath("$PSScriptRoot/../../artifacts/H03/inspection")
[IO.Directory]::CreateDirectory($outputRoot) | Out-Null
$zip = [IO.Compression.ZipFile]::OpenRead([IO.Path]::GetFullPath($PackagePath))
try {
    $targets = @('netstandard2.0','net48','net481','net8.0','net9.0')
    $documents = @('LICENSE','README.md','CHANGELOG.md','docs/PROVIDERS.md','docs/DEPENDENCIES.md')
    $allowed = @('GetCNPJ.Community.nuspec','_rels/.rels','[Content_Types].xml') + $documents
    $allowed += $targets | ForEach-Object { "lib/$_/GetCNPJ.dll" }
    if ($zip.Entries.Count -ne 14) { throw 'Esperadas exatamente14 entradas' }
    if (@($zip.Entries.FullName | Select-Object -Unique).Count -ne 14) { throw 'Entradas duplicadas' }
    foreach ($name in $allowed) { if (-not $zip.GetEntry($name)) { throw "Ausente: $name" } }
    foreach ($entry in $zip.Entries) {
        if ($entry.FullName -notin $allowed -and $entry.FullName -notmatch '^package/services/metadata/core-properties/(?:nuget|[a-f0-9]+)\.psmdcp$') { throw "Arquivo inesperado: $($entry.FullName)" }
        $stream = $entry.Open()
        $buffer = New-Object IO.MemoryStream
        try { $stream.CopyTo($buffer); $bytes = $buffer.ToArray() } finally { $stream.Dispose(); $buffer.Dispose() }
        if ($entry.FullName -in $documents -or $entry.FullName.StartsWith('lib/')) {
            $relative = if ($entry.FullName.StartsWith('lib/')) { $entry.FullName.Replace('lib/','bin/Release/') } else { $entry.FullName }
            $sourceBytes = [IO.File]::ReadAllBytes((Join-Path $ApprovedSource $relative))
            if ([Convert]::ToBase64String($bytes) -ne [Convert]::ToBase64String($sourceBytes)) { throw "Bytes divergentes: $relative" }
        }
        if ($entry.FullName.StartsWith('lib/')) {
            # Apenas caminhos da lista fixa validada acima, sem extração arbitrária do ZIP.
            $destination = Join-Path $outputRoot $entry.FullName
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination)) | Out-Null
            [IO.File]::WriteAllBytes($destination, $bytes)
        } else {
            $xmlText = [Text.Encoding]::UTF8.GetString($bytes).TrimStart([char]0xFEFF)
            if ($entry.FullName -eq 'GetCNPJ.Community.nuspec') {
                [IO.File]::WriteAllBytes((Join-Path $outputRoot 'package.nuspec'), $bytes)
                [xml]$nuspec = $xmlText
            } elseif ($entry.FullName -notin $documents) {
                [xml]$opc = $xmlText
                if (-not $opc.DocumentElement) { throw 'Metadado OPC inválido' }
            }
        }
    }
    $m = $nuspec.package.metadata
    if ($m.id -ne 'GetCNPJ.Community' -or $m.version -ne '1.1.0' -or $m.title -ne 'GetCNPJ Community') { throw 'Identidade' }
    if ($m.authors -ne 'Bruno da Cunha Felipe,Thalison Santos' -or $m.copyright -ne 'Copyright (c) 2025 Bruno da Cunha Felipe') { throw 'Créditos' }
    if ($m.license.type -ne 'expression' -or $m.license.InnerText -ne 'MIT' -or $m.readme -ne 'README.md' -or $m.icon) { throw 'Licença/README/logo' }
    if ($m.repository.type -ne 'git' -or $m.repository.url -ne 'https://github.com/ThalisonSantos/GetCNPJ' -or $m.repository.commit -ne $commit -or $m.projectUrl -ne $m.repository.url) { throw 'Proveniência/URLs' }
    [xml]$project = Get-Content -Raw -Encoding UTF8 (Join-Path $ApprovedSource 'GetCNPJ.csproj')
    if ($m.description -ne $project.Project.PropertyGroup.Description -or $m.releaseNotes -ne $project.Project.PropertyGroup.PackageReleaseNotes -or
        $m.tags -ne $project.Project.PropertyGroup.PackageTags.Replace(';',' ')) { throw 'Descricao/notas/tags' }
    if (@($m.dependencies.group).Count -ne 5) { throw 'Frameworks dependências' }
    foreach ($group in $m.dependencies.group) {
        if ($group.targetFramework -in @('net8.0','net9.0')) {
            if ($group.dependency) { throw 'Dependência moderna redundante' }
        } elseif (@($group.dependency).Count -ne 1 -or $group.dependency.id -ne 'System.Text.Json' -or $group.dependency.version -ne '8.0.6') { throw 'Dependência legada' }
    }
    if ($m.frameworkAssemblies.frameworkAssembly.assemblyName -ne 'System.Net.Http' -or $m.frameworkAssemblies.frameworkAssembly.targetFramework -ne '.NETFramework4.8, .NETFramework4.8.1') { throw 'Framework HTTP' }
    $evidence = [ordered]@{
        Package = [IO.Path]::GetFullPath($PackagePath); Bytes = (Get-Item -LiteralPath $PackagePath).Length
        SHA256 = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
        RepositoryCommit = $m.repository.commit; Entries = @($zip.Entries | ForEach-Object { $_.FullName })
    }
    $evidence | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $outputRoot 'evidence.json')
    $evidence | ConvertTo-Json -Depth 4
    'INSPEÇÃO ZIP/NUSPEC APROVADA; conferir InformationalVersion das cinco DLLs com ModernConsumer.'
} finally { $zip.Dispose() }
