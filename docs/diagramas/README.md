# Diagrama de componentes: DSC.Tech.Hypatia

**Decisão relacionada:** ADR-001, biblioteca própria e leve de mensageria e CQRS, sem dependência de MediatR (`ADR-001-Hypatia.docx`, 19/10/2026).
**Status:** rascunho em revisão pelo autor (Sprint 2, diagrama C4 nível 3 simplificado).
**Ferramenta:** Mermaid (gratuita e de código aberto). O diagrama é texto, então cada mudança aparece no histórico de commits.

![Diagrama de componentes do Hypatia](hypatia-componentes.png)

## Leitura do fluxo

1. O chamador (controller, endpoint ou job) envia a mensagem ao `IMessageDispatcher` (`Send` ou `Query`).
2. `IMessageDispatcher` é implementado por `MessageDispatcher`, registrado pelo `AddHypatia` como Scoped.
3. O `MessageDispatcher` resolve o handler no escopo corrente do contêiner de DI do .NET.
4. Se houver exatamente um handler, o dispatcher invoca `Handle(mensagem, CancellationToken)`. Se houver nenhum ou mais de um, lança `HandlerNotFoundException` ou `MultipleHandlersException`.

O `AddHypatia(params Assembly[])` varre os assemblies informados e registra o dispatcher e os handlers no contêiner.

## Legenda

- **Verde:** componentes do Hypatia, agrupados nas camadas Abstractions, Dispatching e DependencyInjection.
- **Azul:** código da aplicação consumidora. As interfaces `ICommand`, `ICommand<TResult>`, `IQuery<TResult>`, `ICommandHandler` e `IQueryHandler` pertencem à camada Abstractions; no diagrama aparecem do lado da aplicação, que as implementa.
- **Cinza:** dependência externa (`Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.0).

## Código-fonte do diagrama

```mermaid
%%{init: {"theme":"base","themeVariables":{"fontFamily":"Arial","fontSize":"15px","clusterBkg":"#f7fbf9","clusterBorder":"#00b050","edgeLabelBackground":"#ffffff","lineColor":"#4d4d4d"},"flowchart":{"curve":"linear","nodeSpacing":45,"rankSpacing":80,"padding":14}}}%%
flowchart LR
    CALLER["<b>Chamador</b><br/>Controller, endpoint ou job<br/>(aplicação consumidora)"]

    subgraph HYP["Hypatia: pacote NuGet diegoschagas.Hypatia 2.0.2"]
        direction LR
        subgraph ABS["Abstractions (contratos públicos)"]
            IMD["<b>IMessageDispatcher</b><br/>Send, Send#lt;TResult#gt;,<br/>Query#lt;TResult#gt;"]
        end
        subgraph DSP["Dispatching"]
            MD["<b>MessageDispatcher</b><br/>exatamente um handler<br/>por mensagem"]
            EXC["HandlerNotFoundException<br/>MultipleHandlersException"]
        end
        subgraph DIJ["DependencyInjection"]
            ADD["<b>AddHypatia(params Assembly[])</b><br/>varre assemblies<br/>e registra como Scoped"]
        end
    end

    HND["<b>Handlers dos casos de uso</b><br/>implementam ICommandHandler<br/>e IQueryHandler<br/>(aplicação consumidora)"]
    MSG["<b>Mensagens</b><br/>implementam ICommand,<br/>ICommand#lt;TResult#gt; e<br/>IQuery#lt;TResult#gt;<br/>(aplicação consumidora)"]
    DI[("Contêiner de DI do .NET<br/>Microsoft.Extensions<br/>.DependencyInjection<br/>.Abstractions 10.0.0")]

    CALLER -->|"1. Send / Query"| IMD
    IMD -->|"2. implementado por"| MD
    MD -->|"3. resolve o handler<br/>no escopo corrente"| DI
    MD -->|"4. Handle(mensagem,<br/>CancellationToken)"| HND
    DI ~~~ HND
    MD -.->|"0 ou mais de 1 handler"| EXC
    ADD -->|"registra dispatcher<br/>e handlers"| DI
    HND -.->|"tratam"| MSG

    classDef ext fill:#f2f2f2,stroke:#7f7f7f,color:#1a1f20;
    classDef lib fill:#e8f7ee,stroke:#00b050,color:#1a1f20;
    classDef app fill:#eaf1fb,stroke:#4a6fa5,color:#1a1f20;
    class DI ext;
    class IMD,MD,EXC,ADD lib;
    class CALLER,MSG,HND app;
```

Para editar: altere o bloco acima (ou o arquivo `hypatia-componentes.mmd`) e confira o resultado em https://mermaid.live. O GitHub renderiza blocos `mermaid` em arquivos Markdown.

## Arquivo draw.io: mapa do projeto e diagramas da Sprint 2

O arquivo `hypatia-mapa-do-projeto.drawio` tem 6 páginas e abre em https://app.diagrams.net/ (Arquivo > Abrir de > Dispositivo, ou arrastar o arquivo para a janela). É um rascunho em andamento e serve como evidência de execução.

| Página | Conteúdo |
| --- | --- |
| 1 · Mapa do projeto | Objetivo SMART, 3 sprints, situação de cada entrega (concluído, em andamento, planejado) e onde está cada evidência |
| 2 · C4 nível 1 | Contexto: pessoas, aplicação consumidora, GitHub, NuGet.org e contêiner de DI |
| 3 · C4 nível 2 | Contêineres e fluxo de distribuição, com a lacuna do `.consumer-test` fora do workflow |
| 4 · C4 nível 3 | Componentes da biblioteca: Abstractions, Dispatching e DependencyInjection |
| 5 · Sequência | Send com retorno (Send sem retorno e Query seguem o mesmo fluxo) |
| 6 · Sequência | Handler ausente ou duplicado, e registro com `AddHypatia` |

Para a evidência no relatório, abra cada página no draw.io e use Arquivo > Exportar como > PNG, ou tire um print com a barra de endereço visível.
