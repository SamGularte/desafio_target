# 01 — Comissão de vendas

Programa em C# (.NET 8) que lê um JSON com as vendas de um time comercial, calcula a
comissão de cada vendedor e gera um relatório no console e em um arquivo `.txt`.

## Regra de comissão

Aplicada **venda a venda**, depois somada por vendedor:

| Faixa da venda | Comissão |
|---|---|
| abaixo de R$ 100,00 | 0% (nenhuma) |
| de R$ 100,00 até R$ 499,99 | 1% |
| a partir de R$ 500,00 | 5% |

Os limites estão nas constantes `LimiteDeIsencao`, `LimiteDeComissaoPadrao`,
`PercentualPadrao` e `PercentualMaximo` (`Program.cs:8-11`) e a aplicação acontece em
`CalcularComissao` (`Program.cs:116-121`), com `switch` expression:

```csharp
private static decimal CalcularComissao(Venda venda) => venda.Valor switch
{
    < LimiteDeIsencao => 0m,
    < LimiteDeComissaoPadrao => venda.Valor * PercentualPadrao,
    _ => venda.Valor * PercentualMaximo
};
```

## Como executar

```bash
dotnet run -- vendas.txt
```

O caminho do arquivo também pode ser digitado ao vivo (o programa pede) ou arrastado
para o terminal — aspas são removidas automaticamente.

Saída esperada:

```
Vendedor               Qtd     Total vendido          Comissão
--------------------------------------------------------------
João Silva              10      R$ 10.754,70         R$ 495,68
Maria Souza              9       R$ 9.874,30         R$ 465,95
Ana Lima                 9       R$ 8.763,95         R$ 404,98
Carlos Oliveira          8       R$ 7.928,35         R$ 379,37
TOTAL                   36      R$ 37.321,30       R$ 1.745,98
Relatório salvo em: ...\01-comissao-vendas\relatorio-comissao.txt
```

## Arquivo gerado

Toda execução bem-sucedida grava **`relatorio-comissao.txt` nesta pasta** (junto ao
`Program.cs`), sobrescrevendo a versão anterior. O conteúdo é exatamente a tabela
mostrada no console, salvo em UTF-8. A pasta de destino é resolvida subindo a partir do
binário até encontrar o `.csproj`, então o relatório nunca cai dentro de `bin/` nem na
pasta de outro desafio.

## Estrutura do código

| Método | Responsabilidade |
|---|---|
| `Main` | orquestra: caminho → leitura → cálculo → relatório → exit code |
| `ObterCaminhoDoArquivo` / `PedirCaminhoAoUsuario` | aceita argumento de linha de comando ou pergunta ao usuário |
| `LerVendas` + `Validar` | desserializa o JSON e rejeita dados inválidos |
| `CalcularComissao` | aplica a regra da tabela acima em uma venda |
| `CalcularComissoesPorVendedor` | agrupa por vendedor (ignorando maiúsculas/minúsculas) e ordena pela maior comissão |
| `GerarRelatorio` / `EscreverRelatorio` | monta a tabela alinhada em um `StringWriter` |
| `SalvarRelatorio` / `ObterPastaDoProjeto` | grava o `.txt` na pasta deste projeto |

Tipos de dados no fim de `Program.cs`:

- `Venda` — uma linha do JSON (`vendedor`, `valor`)
- `ResumoVendedor` — agregação por vendedor (quantidade, total vendido, comissão)
- `DocumentoDeVendas` — raiz do JSON (`vendas`)
- `DadosDeVendaInvalidosException` — erro de domínio para dados inconsistentes

## Decisões de projeto

- **`decimal` para dinheiro** — `double` acumularia erro de ponto flutuante.
- **`JsonSerializerDefaults.Web`** — casa os nomes em `camelCase` do JSON
  (`vendedor`, `valor`) com as propriedades em PascalCase, sem atributos.
- **Arredondamento apenas na impressão** (`N2` em `pt-BR`) — a soma é feita com o valor
  exato e arredondada uma única vez no relatório, evitando acréscimo de erro por venda.
- **Agrupamento com `StringComparer.OrdinalIgnoreCase`** — `"João Silva"` e
  `"joão silva"` são o mesmo vendedor.
- **Exit codes** — `0` em sucesso, `1` quando o caminho não existe (o programa tenta
  perguntar de novo; sem entrada disponível ele encerra), JSON inválido ou venda com
  valor negativo — sempre com mensagem em `Console`, sem stack trace.
- **Validação explícita** — vendedor vazio ou valor negativo abortam o processamento em
  vez de gerar comissão silenciosamente.

## Validações feitas

- Bordas da regra: `99,99 → R$ 0`, `100,00 → 1%`, `499,99 → 1%`, `500,00 → 5%`.
- JSON malformado e valor negativo → mensagem de erro e exit code `1`.
- Compilação com 0 avisos e 0 erros (`dotnet build`).
