# Quality Gate — DSC.Tech.Hypatia

- Data: 18/11/2026 11:13
- Máquina: TATOOINE (Microsoft Windows NT 10.0.22621.0)
- .NET SDK: 10.0.404
- Commit: 7744ddf (arquivos alterados não commitados: 9)
- Versão de teste do pacote: 2.0.2-qg.202611181113
- Resultado geral: **APROVADO**

| Critério | Meta | Resultado | Situação |
|---|---|---|---|
| Build Release sem warnings | 0 warnings e 0 erros | Warnings: 0; erros: 0 | APROVADO |
| Testes unitários e de integração | 100% aprovados | 24 de 24 aprovados (0 falhas) | APROVADO |
| Cobertura de linhas | >= 90% | 98.43% (63 de 64 linhas) | APROVADO |
| Cobertura de ramos | registrar | 95% | REGISTRADO |
| Pacote NuGet | .nupkg gerado | diegoschagas.Hypatia.2.0.2-qg.202611181113.nupkg | APROVADO |
| Consumidor externo | executa com o pacote gerado | HYPATIA_CONSUMER_OK impresso: True; restaurado da versão 2.0.2-qg.202611181113: True | APROVADO |
| Dependências | zero MediatR/LuckyPenny | Ocorrências: 0 | APROVADO |

## Dependências do pacote (nuspec)
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0

## Pacotes da biblioteca (diretos e transitivos)
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0 10.0.0 

## Etapas
| Etapa | Exit code | Tempo (s) | Log |
|---|---|---|---|
| Restore | 0 | 7.4 | logs/01-restore.log |
| Build Release (warnings como erros) | 0 | 26.6 | logs/02-build.log |
| Testes com cobertura | 0 | 22.1 | logs/03-test.log |
| Pack (2.0.2-qg.202611181113) | 0 | 5 | logs/04-pack.log |
| Consumidor externo: restore | 0 | 5.3 | logs/05-consumer-restore.log |
| Consumidor externo: execução | 0 | 7.9 | logs/06-consumer-run.log |
| Dependências: biblioteca | 0 | 7.4 | logs/07-deps-lib.log |
| Dependências: testes | 0 | 12.7 | logs/08-deps-tests.log |
| Dependências: consumidor | 0 | 5.5 | logs/09-deps-consumer.log |
| Benchmark: criar projeto | 0 | 6.8 | logs/10-bench-new.log |
| Benchmark: referência à biblioteca | 0 | 2.1 | logs/11-bench-ref.log |
| Benchmark: BenchmarkDotNet | 0 | 23.4 | logs/12-bench-pkg.log |
| Benchmark: execução (alguns minutos) | 0 | 400.3 | logs/13-bench-run.log |

## Benchmark do despacho por reflexão (H5)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22621.4317/22H2/2022Update/SunValley2)
Intel Core i7-5500U CPU 2.40GHz (Broadwell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method            | Mean          | Error       | StdDev      | Median        | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------------ |--------------:|------------:|------------:|--------------:|---------:|---------:|-------:|----------:|------------:|
| DirectHandler     |     0.8079 ns |   0.1630 ns |   0.4806 ns |     0.7082 ns |     1.49 |     1.43 |      - |         - |          NA |
| SendWithoutResult | 1,249.6873 ns |  65.4978 ns | 193.1217 ns | 1,171.3464 ns | 2,300.33 | 1,556.91 | 0.1297 |     272 B |          NA |
| SendWithResult    | 1,562.6783 ns |  92.3961 ns | 272.4319 ns | 1,622.1292 ns | 2,876.46 | 1,966.26 | 0.2098 |     440 B |          NA |
| Query             | 1,310.2691 ns | 113.0751 ns | 333.4044 ns | 1,246.5860 ns | 2,411.84 | 1,732.03 | 0.2098 |     440 B |          NA |


