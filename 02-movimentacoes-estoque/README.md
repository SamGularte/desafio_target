# 02 — Movimentações de estoque

Programa em C# (.NET 8) que lê um JSON com o estoque inicial dos produtos e uma lista de
movimentações (entrada/saída de mercadoria), processa cada movimentação na ordem e mostra
**a quantidade final do estoque do produto movimentado**.

## Regras

Cada movimentação tem:

- **Identificador único** — gerado automaticamente pelo programa, sequencial a partir de
  `1`, na ordem em que as movimentações aparecem no arquivo.
- **Descrição que identifica o tipo** — `ENTRADA`, `SAIDA` ou `SAÍDA` (comparação exata,
  sem regra implícita: qualquer outro texto é recusado).
- **Produto e quantidade** — a quantidade deve ser maior que zero.

Regras de negócio:

| Regra | Comportamento |
|---|---|
| Entrada | soma a quantidade ao saldo do produto |
| Saída | subtrai a quantidade do saldo do produto |
| Saída maior que o saldo disponível | movimentação recusada (erro, exit code 1) |
| Produto inexistente / quantidade ≤ 0 / descrição inválida | movimentação recusada (erro, exit code 1) |
| Sem `movimentacoes` no arquivo | relatório mostra só o estoque final, sem erro |

## Arquivo de entrada

`estoque.txt` — o estoque inicial do desafio mais as movimentações de exemplo:

```json
{
  "estoque": [
    { "codigoProduto": 101, "descricaoProduto": "Caneta Azul", "estoque": 150 }
  ],
  "movimentacoes": [
    { "codigoProduto": 101, "quantidade": 50, "descricao": "ENTRADA" },
    { "codigoProduto": 101, "quantidade": 25, "descricao": "SAIDA" }
  ]
}
```

Para lançar outras movimentações, basta editar a lista `movimentacoes` (ou passar outro
arquivo). A seção `movimentacoes` é opcional: se o arquivo tiver apenas o `estoque`, o
programa valida e imprime o estoque atual.

## Como executar

```bash
# sem argumento: usa o estoque.txt desta pasta automaticamente
dotnet run --project 02-movimentacoes-estoque

# ou apontando para outro arquivo
dotnet run --project 02-movimentacoes-estoque -- caminho/para/estoque.txt
```

Saída esperada:

```
  Id  Descrição                         Produto                              Qtd      Saldo após
----  --------------------------------  ------------------------------  --------  --------------
   1  Entrada                           Caneta Azul (101)                    +50             200
   2  Saída                             Caneta Azul (101)                    -25             175
 ...

  Código  Produto                                Inicial        Entradas      Saídas   Saldo final
--------  ------------------------------  --------------  --------------  ----------  ------------
     101  Caneta Azul                                150              50          25           175
 ...
          TOTAL                                      835             255         345           745
Relatório salvo em: ...\02-movimentacoes-estoque\relatorio-estoque.txt
```

A primeira tabela traz **o saldo depois de cada movimentação**; a segunda traz o estoque
final de todos os produtos, com totais de entradas e saídas.

## Arquivo gerado

Toda execução bem-sucedida grava **`relatorio-estoque.txt` nesta pasta**, sobrescrevendo a
versão anterior, com o mesmo conteúdo exibido no console (UTF-8). A pasta de destino é
resolvida subindo a partir do binário até encontrar o `.csproj`, então o relatório nunca
cai dentro de `bin/` nem na pasta de outro desafio.

## Estrutura do código

| Método | Responsabilidade |
|---|---|
| `Main` | orquestra: caminho → leitura → preparo → processamento → relatório → exit code |
| `ObterCaminhoDoArquivo` / `CaminhoPadrao` | usa o argumento da linha de comando ou o `estoque.txt` desta pasta |
| `LerDocumento` + `ValidarProduto` | desserializa o JSON, valida produtos e detecta códigos duplicados |
| `PrepararMovimentacoes` + `ConverterTipo` | valida cada movimentação e gera o id sequencial único |
| `Processar` | aplica entrada/saída na ordem, valida saldo e acumula os totais por produto |
| `GerarRelatorio` / `Escrever*` | monta as duas tabelas alinhadas num `StringWriter` |
| `SalvarRelatorio` / `ObterPastaDoProjeto` | grava o `.txt` na pasta deste projeto |

Tipos de dados no fim de `Program.cs`:

- `Documento` — raiz do JSON (`estoque` + `movimentacoes`)
- `Produto` — item do estoque (código, descrição, estoque inicial)
- `MovimentacaoBruta` — movimentação como veio do JSON (`descricao`, produto, quantidade)
- `Movimentacao` — movimentação já validada, com `Id` único e `Tipo` (enum `TipoMovimentacao`)
- `ResultadoMovimentacao` / `ResumoProduto` / `ResultadoProcessamento` — dados das tabelas
- `DadosDeEstoqueInvalidosException` — erro de domínio para dados inconsistentes

## Decisões de projeto

- **Id gerado pelo programa** — garantir unicidade não depende do arquivo de entrada.
- **`int` para quantidades** — unidades inteiras de produto; `decimal` só seria necessário
  se houvesse valores monetários.
- **Fail fast nas movimentações inválidas** — dados inconsistentes abortam com mensagem e
  exit code `1`, sem gravar relatório parcial (mesma postura do desafio 01).
- **Processamento sequencial** — o saldo de cada movimentação depende do anterior, então a
  ordem do arquivo é preservada.
- **Sem argumento, cai no `estoque.txt` da pasta** — `dotnet run` puro já funciona, sem
  pendurar em prompt esperando entrada.
- **Comparação exata na descrição** — `ENTRADA`/`SAIDA`/`SAÍDA` na íntegra; qualquer outro
  texto é recusado, sem `Contains` escondendo regra implícita.
- **Agrupamento por código de produto** — o relatório final usa `Dictionary` para não
  varrer a lista inteira a cada movimentação.

## Validações feitas

- Caminhos críticos: saída maior que o saldo, produto inexistente, quantidade `0`,
  descrição que não é `ENTRADA`/`SAIDA`, campo `descricao` ausente → todos com erro claro e
  exit code `1`.
- `SAÍDA` com acento → aceito.
- Arquivo só com `estoque` (JSON do enunciado, sem movimentações) → exit code `0`.
- JSON malformado → mensagem de erro e exit code `1`.
- Consistência dos números: `835` iniciais `+ 255` entradas `- 345` saídas `= 745` final.
- Compilação com 0 avisos e 0 erros (`dotnet build`).
