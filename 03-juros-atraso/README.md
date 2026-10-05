# 03 — Juros por atraso

Programa em C# (.NET 8) que, a partir de **um valor** e **uma data de vencimento**,
calcula o valor dos juros na data de hoje, com multa de **2,5% ao dia**, e mostra quanto
pagar.

## Regra de cálculo

```
diasEmAtraso = max(hoje − vencimento, 0)      dias corridos, sem contar vencimento
juros        = valor × 2,5% × diasEmAtraso    juros simples (multa de mora)
total        = valor + juros
```

- **Juros simples**: a taxa incide sempre sobre o valor original, uma vez por dia de
  atraso — não capitaliza.
- **Sem atraso** (vence hoje ou no futuro): `diasEmAtraso = 0` e juros `R$ 0,00`.
- A taxa está na constante `TaxaDiaria = 0.025m` (`Program.cs:8`); a aplicação em
  `Calcular` (`Program.cs:173`).

## Como executar

O usuário digita os dados — o programa não lê nenhum arquivo:

```bash
dotnet run --project 03-juros-atraso
```

```
Informe os dados do título.

Valor: 150,50
Vencimento (dd/MM/aaaa): 20/09/2026
```

Um valor/data inválido mostra o erro e **pergunta de novo** (até acertar ou `Ctrl+C`);
se o terminal não tiver entrada (input fechado), ele encerra com código `1` em vez de
travar.

Para automação/testes, os mesmos dois dados podem vir na linha de comando:

```bash
dotnet run --project 03-juros-atraso -- 150,50 20/09/2026
```

> **PowerShell (modo argumento):** valores com milhar têm de ir entre aspas — o shell
> interpreta a vírgula como separador de lista (`-- "1.500,50" 01/10/2026`). No
> cmd/WSL/Git Bash não é necessário.

Quantidade de argumentos diferente de 2 (ex.: só `100`, ou três argumentos) imprime o uso
e encerra com código `1`.

Saída esperada:

```
CÁLCULO DE JUROS POR ATRASO
------------------------------------

Valor original             R$ 150,50
Vencimento                20/09/2026
Data do cálculo           05/10/2026
Dias em atraso                    15
Multa (2,5% ao dia)         R$ 56,44
Total a pagar              R$ 206,94
Relatório salvo em: ...\03-juros-atraso\relatorio-juros.txt
```

(Os dias dependem do dia em que você rodar — `Data do cálculo` mostra a data usada.)

## Arquivo gerado

Toda execução bem-sucedida grava **`relatorio-juros.txt` nesta pasta**, sobrescrevendo a
versão anterior, com o mesmo conteúdo exibido no console (UTF-8). A pasta de destino é
resolvida subindo a partir do binário até encontrar o `.csproj`, então o relatório nunca
cai dentro de `bin/`.

## Estrutura do código

| Método | Responsabilidade |
|---|---|
| `Main` | orquestra: entrada → cálculo → relatório → exit code |
| `LerEntrada` / `LerEntradaPeloConsole` / `PedirValor` / `PedirVencimento` | pergunta valor e data no console, repetindo até serem válidos |
| `LerEntradaDosArgumentos` | mesmo par de dados recebido pela linha de comando (modo automação) |
| `ValidarValor` / `ValidarData` / `TryConverterValor` / `TryConverterData` | valida e converte (`decimal` e `DateTime`) nos dois modos |
| `Calcular` | aplica a fórmula dos juros simples |
| `GerarRelatorio` / `EscreverCampo` | monta o bloco de saída num `StringWriter` |
| `SalvarRelatorio` / `ObterPastaDoProjeto` | grava o `.txt` na pasta deste projeto |

Tipo de dados no fim de `Program.cs`:

- `CalculoDeJuros` — valor, vencimento, data do cálculo, dias em atraso e juros; o
  `Total` é derivado (`ValorOriginal + Juros`).

## Decisões de projeto

- **Juros simples** — multa de mora diária no Brasil se calcula sobre o valor original;
  juros compostos mudariam o resultado a partir do 2º dia.
- **O usuário digita os dados** — sem arquivo de entrada: o programa pergunta `Valor:` e
  `Vencimento:` no console; argumentos na linha de comando são atalho opcional para
  testes/automação.
- **Datas com formato exato** (`TryParseExact`) — nada de depender do formato regional da
  máquina: `20/09/2026` e `2026-09-20` são aceitos, qualquer outro é recusado.
- **`decimal` para dinheiro** — evita erro de ponto flutuante; arredondamento só na
  impressão (`N2` em `pt-BR`).
- **`DateTime.Today`** — a data do cálculo é sempre a data do sistema e aparece no
  relatório, deixando o resultado auditável.
- **Exit codes** — `0` em sucesso, `1` para uso errado ou argumento inválido, com
  mensagem em `Console` e sem stack trace.

## Validações feitas

- Digitando `150,50` e `20/09/2026` no console (15 dias de atraso) → `R$ 56,44` de
  juros e `R$ 206,94` no total.
- Entradas inválidas no console (`abc`, `0`, `-5`, `31/02/2026`) → mostra o erro e **pede
  de novo**; em seguida, dados válidos → cálculo normal, exit `0`.
- Vencimento hoje e no futuro → `0` dias e `R$ 0,00` de juros, exit `0`.
- Sem entrada disponível (stdin fechado) → encerra com exit `1`, sem travar.
- Via argumentos: `100` (1 arg) e `100 01/10/2026 extra` (3 args) → usage e exit `1`;
  `100 extra` → erro da data e exit `1`.
- Formatos `150.50` e `150,50` / `1.500,50` → mesmo resultado.
- Compilação com 0 avisos e 0 erros (`dotnet build`).
