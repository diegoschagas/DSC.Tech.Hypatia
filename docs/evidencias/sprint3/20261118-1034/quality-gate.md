# Quality Gate — DSC.Tech.Hypatia

- Data: 18/11/2026 10:34
- Máquina: TATOOINE (Microsoft Windows NT 10.0.22621.0)
- .NET SDK: 10.0.404
- Commit: 7744ddf (arquivos alterados não commitados: 1)
- Versão de teste do pacote: 2.0.2-qg.202611181034
- Resultado geral: **APROVADO**

| Critério | Meta | Resultado | Situação |
|---|---|---|---|
| Build Release sem warnings | 0 warnings e 0 erros | Warnings: 0; erros: 0 | APROVADO |
| Testes unitários e de integração | 100% aprovados | 17 de 17 aprovados (0 falhas) | APROVADO |
| Cobertura de linhas | >= 90% | 98.43% (63 de 64 linhas) | APROVADO |
| Cobertura de ramos | registrar | 90% | REGISTRADO |
| Pacote NuGet | .nupkg gerado | diegoschagas.Hypatia.2.0.2-qg.202611181034.nupkg | APROVADO |
| Consumidor externo | executa com o pacote gerado | HYPATIA_CONSUMER_OK impresso: True; restaurado da versão 2.0.2-qg.202611181034: True | APROVADO |
| Dependências | zero MediatR/LuckyPenny | Ocorrências: 0 | APROVADO |

## Dependências do pacote (nuspec)
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0

## Pacotes da biblioteca (diretos e transitivos)
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.0 10.0.0 

## Etapas
| Etapa | Exit code | Tempo (s) | Log |
|---|---|---|---|
| Restore | 0 | 5.1 | logs/01-restore.log |
| Build Release (warnings como erros) | 0 | 11.6 | logs/02-build.log |
| Testes com cobertura | 0 | 15.3 | logs/03-test.log |
| Pack (2.0.2-qg.202611181034) | 0 | 2.5 | logs/04-pack.log |
| Consumidor externo: restore | 0 | 3.5 | logs/05-consumer-restore.log |
| Consumidor externo: execução | 0 | 4.7 | logs/06-consumer-run.log |
| Dependências: biblioteca | 0 | 4.8 | logs/07-deps-lib.log |
| Dependências: testes | 0 | 8.9 | logs/08-deps-tests.log |
| Dependências: consumidor | 0 | 3.7 | logs/09-deps-consumer.log |

