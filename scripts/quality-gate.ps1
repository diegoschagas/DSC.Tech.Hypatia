<#
.SYNOPSIS
  Quality Gate do DSC.Tech.Hypatia (Sprint 3).

.DESCRIPTION
  Executa, em sequência: restore, build Release com warnings como erros, testes com
  cobertura, pack de uma versão de teste (2.0.2-qg.<data>), consumidor externo restaurado
  a partir desse pacote, varredura de dependências (MediatR/LuckyPenny) e, com -Benchmark,
  o benchmark do despacho por reflexão (hipótese H5).

  Gera docs/evidencias/sprint3/<data-hora>/quality-gate.md com o resumo e a pasta logs/
  com a saída bruta de cada etapa. Nenhum arquivo versionado é alterado: o consumidor e o
  benchmark rodam em uma pasta temporária.

.EXAMPLE
  pwsh ./scripts/quality-gate.ps1 -Benchmark
#>
param(
    [switch]$Benchmark,
    [double]$MinLineCoverage = 90
)

$ErrorActionPreference = 'Continue'
$env:DOTNET_CLI_UI_LANGUAGE = 'en'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$now       = Get-Date
$stamp     = $now.ToString('yyyyMMdd-HHmm')
$qgVersion = '2.0.2-qg.' + $now.ToString('yyyyMMddHHmm')
$out       = Join-Path $root "docs/evidencias/sprint3/$stamp"
$logs      = Join-Path $out 'logs'
$work      = Join-Path ([IO.Path]::GetTempPath()) "hypatia-qg-$stamp"
New-Item -ItemType Directory -Force $out, $logs, $work | Out-Null

$steps = New-Object System.Collections.Generic.List[object]
function LastExit { $steps[$steps.Count - 1].ExitCode }
function FirstGroup($lines, $pattern) {
    $m = $lines | Select-String $pattern | Select-Object -Last 1
    if ($m) { $m.Matches[0].Groups[1].Value } else { 'n/d' }
}

function Invoke-Step {
    param([string]$Name, [string]$Log, [string[]]$DotnetArgs, [string]$WorkDir = $root)
    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    Write-Host "    dotnet $($DotnetArgs -join ' ')" -ForegroundColor DarkGray
    $sw = [Diagnostics.Stopwatch]::StartNew()
    Push-Location $WorkDir
    $output = & dotnet @DotnetArgs 2>&1 | ForEach-Object { "$_" }
    $code = $LASTEXITCODE
    Pop-Location
    $sw.Stop()
    $output | Set-Content -Encoding utf8 (Join-Path $logs $Log)
    $output | Select-Object -Last 15 | Out-Host
    $status = if ($code -eq 0) { 'OK' } else { 'FALHOU' }
    Write-Host "    $status (exit $code, $([math]::Round($sw.Elapsed.TotalSeconds,1)) s)" -ForegroundColor $(if ($code -eq 0) { 'Green' } else { 'Red' })
    $steps.Add([pscustomobject]@{ Name = $Name; ExitCode = $code; Seconds = [math]::Round($sw.Elapsed.TotalSeconds, 1); Log = $Log })
    return ,$output
}

# ---------------------------------------------------------------- ambiente
$dotnetVersion = (& dotnet --version) 2>&1 | Select-Object -First 1
$commit        = (& git rev-parse --short HEAD) 2>&1 | Select-Object -First 1
$dirty         = @(& git status --porcelain 2>$null).Count
$os            = [Environment]::OSVersion.VersionString

# ---------------------------------------------------------------- 1. restore + build
Invoke-Step 'Restore' '01-restore.log' @('restore', 'DSC.Tech.Hypatia.slnx', '-tl:off') | Out-Null
$buildOut = Invoke-Step 'Build Release (warnings como erros)' '02-build.log' @(
    'build', 'DSC.Tech.Hypatia.slnx', '-c', 'Release', '--no-restore', '-warnaserror', '-tl:off')
$warnings = FirstGroup $buildOut '^\s*(\d+) Warning\(s\)'
$errors   = FirstGroup $buildOut '^\s*(\d+) Error\(s\)'
$buildOk  = (LastExit) -eq 0

# ---------------------------------------------------------------- 2. testes + cobertura
$testDir = Join-Path $out 'testes'
Invoke-Step 'Testes com cobertura' '03-test.log' @(
    'test', 'DSC.Tech.Hypatia.slnx', '-c', 'Release', '--no-build',
    '--collect:XPlat Code Coverage', '--results-directory', $testDir,
    '--logger', 'trx;LogFileName=tests.trx', '-tl:off') | Out-Null
$testOk = (LastExit) -eq 0

$total = $passed = $failed = 'n/d'
$trx = Get-ChildItem $testDir -Recurse -Filter *.trx -ErrorAction SilentlyContinue | Select-Object -First 1
if ($trx) {
    [xml]$t = Get-Content $trx.FullName -Raw
    $c = $t.TestRun.ResultSummary.Counters
    $total = $c.total; $passed = $c.passed; $failed = $c.failed
}

$lineRate = $branchRate = 'n/d'; $linesCovered = $linesValid = 'n/d'
$cob = Get-ChildItem $testDir -Recurse -Filter coverage.cobertura.xml -ErrorAction SilentlyContinue | Select-Object -First 1
if ($cob) {
    [xml]$x = Get-Content $cob.FullName -Raw
    $lineRate     = [math]::Round([double]::Parse($x.coverage.'line-rate',   [Globalization.CultureInfo]::InvariantCulture) * 100, 2)
    $branchRate   = [math]::Round([double]::Parse($x.coverage.'branch-rate', [Globalization.CultureInfo]::InvariantCulture) * 100, 2)
    $linesCovered = $x.coverage.'lines-covered'
    $linesValid   = $x.coverage.'lines-valid'
}
$coverageOk = ($lineRate -ne 'n/d') -and ([double]$lineRate -ge $MinLineCoverage)

# ---------------------------------------------------------------- 3. pack
$licenseCopied = $false
if (-not (Test-Path 'src/LICENSE')) { Copy-Item 'LICENSE' 'src/LICENSE'; $licenseCopied = $true }  # igual ao workflow
$pkgDir = Join-Path $out 'nupkg'
Invoke-Step "Pack ($qgVersion)" '04-pack.log' @(
    'pack', 'src/DSC.Tech.Hypatia/DSC.Tech.Hypatia.csproj', '-c', 'Release', '--no-build',
    "-p:PackageVersion=$qgVersion", '-o', $pkgDir, '-tl:off') | Out-Null
if ($licenseCopied) { Remove-Item 'src/LICENSE' -Force }
$packOk = (LastExit) -eq 0
$nupkg  = Get-ChildItem $pkgDir -Filter *.nupkg -ErrorAction SilentlyContinue | Select-Object -First 1

$nuspecDeps = @()
if ($nupkg) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead($nupkg.FullName)
    try {
        $entry  = $zip.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
        $reader = New-Object IO.StreamReader($entry.Open())
        $nuspec = $reader.ReadToEnd(); $reader.Close()
        $nuspec | Set-Content -Encoding utf8 (Join-Path $logs '04-pack.nuspec.xml')
        $nuspecDeps = [regex]::Matches($nuspec, '<dependency id="([^"]+)" version="([^"]+)"') |
            ForEach-Object { "$($_.Groups[1].Value) $($_.Groups[2].Value)" } | Sort-Object -Unique
    } finally { $zip.Dispose() }
}

# ---------------------------------------------------------------- 4. consumidor externo
$consumer = Join-Path $work 'consumer'
Copy-Item '.consumer-test' $consumer -Recurse
$csproj = Join-Path $consumer 'Hypatia.Consumer.csproj'
(Get-Content $csproj -Raw) -replace '(Include="diegoschagas\.Hypatia"\s+Version=")[^"]+(")', "`${1}$qgVersion`${2}" |
    Set-Content -Encoding utf8 $csproj
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="quality-gate" value="$pkgDir" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="quality-gate"><package pattern="diegoschagas.Hypatia" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content -Encoding utf8 (Join-Path $consumer 'NuGet.Config')

$oldPackages = $env:NUGET_PACKAGES
$env:NUGET_PACKAGES = Join-Path $work 'packages'   # cache isolado: garante o pacote recém-gerado
Invoke-Step 'Consumidor externo: restore' '05-consumer-restore.log' @('restore', '-tl:off') $consumer | Out-Null
$consumerOut = Invoke-Step 'Consumidor externo: execução' '06-consumer-run.log' @('run', '-c', 'Release', '--no-restore') $consumer
$consumerPrinted = @($consumerOut | Where-Object { $_ -match 'HYPATIA_CONSUMER_OK' }).Count -gt 0
$consumerOk = ((LastExit) -eq 0) -and $consumerPrinted
$assets = Get-Content (Join-Path $consumer 'obj/project.assets.json') -Raw -ErrorAction SilentlyContinue
$consumerUsesQg = $assets -match [regex]::Escape("diegoschagas.Hypatia/$qgVersion")

# ---------------------------------------------------------------- 5. dependências
$depLogs = @()
Invoke-Step 'Dependências: biblioteca' '07-deps-lib.log' @('list', 'src/DSC.Tech.Hypatia/DSC.Tech.Hypatia.csproj', 'package', '--include-transitive') | Out-Null
$depLogs += '07-deps-lib.log'
Invoke-Step 'Dependências: testes' '08-deps-tests.log' @('list', 'tests/DSC.Tech.Hypatia.Tests/DSC.Tech.Hypatia.Tests.csproj', 'package', '--include-transitive') | Out-Null
$depLogs += '08-deps-tests.log'
Invoke-Step 'Dependências: consumidor' '09-deps-consumer.log' @('list', 'package', '--include-transitive') $consumer | Out-Null
$depLogs += '09-deps-consumer.log'
$env:NUGET_PACKAGES = $oldPackages

$forbidden = @()
foreach ($l in $depLogs) {
    $forbidden += Select-String -Path (Join-Path $logs $l) -Pattern 'MediatR|LuckyPenny' -ErrorAction SilentlyContinue
}
$forbidden += $nuspecDeps | Select-String 'MediatR|LuckyPenny'
$depsOk = ($forbidden.Count -eq 0) -and ($steps | Where-Object { $_.Name -like 'Dependências*' -and $_.ExitCode -ne 0 }).Count -eq 0
$libPackages = Get-Content (Join-Path $logs '07-deps-lib.log') |
    Where-Object { $_ -match '^\s*>\s+(\S+)' } | ForEach-Object { ($_ -replace '^\s*>\s+', '') -replace '\s{2,}', ' ' }

# ---------------------------------------------------------------- 6. benchmark (H5)
$benchMd = $null
if ($Benchmark) {
    $bench = Join-Path $work 'bench'
    Invoke-Step 'Benchmark: criar projeto' '10-bench-new.log' @('new', 'console', '-n', 'Hypatia.Benchmarks', '-o', $bench, '-f', 'net10.0') | Out-Null
    Invoke-Step 'Benchmark: referência à biblioteca' '11-bench-ref.log' @('add', (Join-Path $bench 'Hypatia.Benchmarks.csproj'), 'reference', (Join-Path $root 'src/DSC.Tech.Hypatia/DSC.Tech.Hypatia.csproj')) | Out-Null
    Invoke-Step 'Benchmark: BenchmarkDotNet' '12-bench-pkg.log' @('add', (Join-Path $bench 'Hypatia.Benchmarks.csproj'), 'package', 'BenchmarkDotNet') | Out-Null
@'
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

BenchmarkRunner.Run<DispatchBenchmarks>();

[MemoryDiagnoser]
public class DispatchBenchmarks
{
    private ServiceProvider _root = null!;
    private IServiceScope _scope = null!;
    private IMessageDispatcher _dispatcher = null!;
    private readonly PingHandler _directHandler = new();
    private readonly Ping _ping = new();
    private readonly NumberCommand _number = new(41);
    private readonly TextQuery _text = new("Hypatia");

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddHypatia(typeof(DispatchBenchmarks).Assembly);
        _root = services.BuildServiceProvider();
        _scope = _root.CreateScope();
        _dispatcher = _scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();
    }

    [GlobalCleanup]
    public void Cleanup() { _scope.Dispose(); _root.Dispose(); }

    [Benchmark(Baseline = true)]
    public Task DirectHandler() => _directHandler.Handle(_ping, CancellationToken.None);

    [Benchmark]
    public Task SendWithoutResult() => _dispatcher.Send(_ping);

    [Benchmark]
    public Task<int> SendWithResult() => _dispatcher.Send<int>(_number);

    [Benchmark]
    public Task<string> Query() => _dispatcher.Query<string>(_text);
}

public sealed record Ping : ICommand;
public sealed record NumberCommand(int Value) : ICommand<int>;
public sealed record TextQuery(string Value) : IQuery<string>;

public sealed class PingHandler : ICommandHandler<Ping>
{
    public Task Handle(Ping command, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class NumberHandler : ICommandHandler<NumberCommand, int>
{
    public Task<int> Handle(NumberCommand command, CancellationToken cancellationToken = default) => Task.FromResult(command.Value + 1);
}

public sealed class TextHandler : IQueryHandler<TextQuery, string>
{
    public Task<string> Handle(TextQuery query, CancellationToken cancellationToken = default) => Task.FromResult(query.Value);
}
'@ | Set-Content -Encoding utf8 (Join-Path $bench 'Program.cs')
    Invoke-Step 'Benchmark: execução (alguns minutos)' '13-bench-run.log' @('run', '-c', 'Release') $bench | Out-Null
    $report = Get-ChildItem (Join-Path $bench 'BenchmarkDotNet.Artifacts/results') -Filter '*-report-github.md' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($report) {
        Copy-Item $report.FullName (Join-Path $out 'benchmark.md')
        $benchMd = Get-Content $report.FullName -Raw
    }
}

# ---------------------------------------------------------------- resumo
function Mark($ok) { if ($ok) { 'APROVADO' } else { 'REPROVADO' } }
$criteria = @(
    @('Build Release sem warnings', '0 warnings e 0 erros', "Warnings: $warnings; erros: $errors", (Mark $buildOk)),
    @('Testes unitários e de integração', '100% aprovados', "$passed de $total aprovados ($failed falhas)", (Mark ($testOk -and "$failed" -eq '0'))),
    @('Cobertura de linhas', ">= $MinLineCoverage%", "$lineRate% ($linesCovered de $linesValid linhas)", (Mark $coverageOk)),
    @('Cobertura de ramos', 'registrar', "$branchRate%", 'REGISTRADO'),
    @('Pacote NuGet', '.nupkg gerado', $(if ($nupkg) { $nupkg.Name } else { 'não gerado' }), (Mark $packOk)),
    @('Consumidor externo', 'executa com o pacote gerado', "HYPATIA_CONSUMER_OK impresso: $consumerPrinted; restaurado da versão ${qgVersion}: $consumerUsesQg", (Mark ($consumerOk -and $consumerUsesQg))),
    @('Dependências', 'zero MediatR/LuckyPenny', "Ocorrências: $($forbidden.Count)", (Mark $depsOk))
)
$gateOk = $buildOk -and $testOk -and "$failed" -eq '0' -and $coverageOk -and $packOk -and $consumerOk -and $consumerUsesQg -and $depsOk

$md = New-Object System.Text.StringBuilder
[void]$md.AppendLine('# Quality Gate — DSC.Tech.Hypatia')
[void]$md.AppendLine('')
[void]$md.AppendLine("- Data: $($now.ToString('dd/MM/yyyy HH:mm'))")
[void]$md.AppendLine("- Máquina: $env:COMPUTERNAME ($os)")
[void]$md.AppendLine("- .NET SDK: $dotnetVersion")
[void]$md.AppendLine("- Commit: $commit (arquivos alterados não commitados: $dirty)")
[void]$md.AppendLine("- Versão de teste do pacote: $qgVersion")
[void]$md.AppendLine("- Resultado geral: **$(Mark $gateOk)**")
[void]$md.AppendLine('')
[void]$md.AppendLine('| Critério | Meta | Resultado | Situação |')
[void]$md.AppendLine('|---|---|---|---|')
foreach ($c in $criteria) { [void]$md.AppendLine("| $($c[0]) | $($c[1]) | $($c[2]) | $($c[3]) |") }
[void]$md.AppendLine('')
[void]$md.AppendLine('## Dependências do pacote (nuspec)')
foreach ($d in $nuspecDeps) { [void]$md.AppendLine("- $d") }
[void]$md.AppendLine('')
[void]$md.AppendLine('## Pacotes da biblioteca (diretos e transitivos)')
foreach ($p in $libPackages) { [void]$md.AppendLine("- $p") }
if ($forbidden.Count -gt 0) {
    [void]$md.AppendLine('')
    [void]$md.AppendLine('## Ocorrências proibidas')
    foreach ($f in $forbidden) { [void]$md.AppendLine("- $f") }
}
[void]$md.AppendLine('')
[void]$md.AppendLine('## Etapas')
[void]$md.AppendLine('| Etapa | Exit code | Tempo (s) | Log |')
[void]$md.AppendLine('|---|---|---|---|')
foreach ($s in $steps) { [void]$md.AppendLine("| $($s.Name) | $($s.ExitCode) | $($s.Seconds) | logs/$($s.Log) |") }
if ($benchMd) {
    [void]$md.AppendLine('')
    [void]$md.AppendLine('## Benchmark do despacho por reflexão (H5)')
    [void]$md.AppendLine('')
    [void]$md.AppendLine($benchMd)
}
$summary = Join-Path $out 'quality-gate.md'
$md.ToString() | Set-Content -Encoding utf8 $summary

Write-Host ""
Write-Host "Resultado geral: $(Mark $gateOk)" -ForegroundColor $(if ($gateOk) { 'Green' } else { 'Red' })
Write-Host "Evidência: $summary"
if ($gateOk) { exit 0 } else { exit 1 }
