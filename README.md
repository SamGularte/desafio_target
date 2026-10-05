# desafio_target

Desafios técnicos da **Target** escritos em **C# / .NET 8**, um por pasta.

## Desafios

| # | Pasta | Assunto | Como rodar |
|---|---|---|---|
| 01 | [`01-comissao-vendas`](01-comissao-vendas/) | Cálculo de comissão de vendas a partir de JSON | `dotnet run --project 01-comissao-vendas -- 01-comissao-vendas/vendas.txt` |
| 02 | [`02-movimentacoes-estoque`](02-movimentacoes-estoque/) | Entrada/saída de mercadoria e saldo final do estoque | `dotnet run --project 02-movimentacoes-estoque` |

Cada pasta contém o seu `Program.cs`, o `.csproj`, o arquivo de dados de entrada e um
`README.md` explicando a solução em detalhes.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Como executar

```bash
dotnet run --project 01-comissao-vendas -- 01-comissao-vendas/vendas.txt
dotnet run --project 02-movimentacoes-estoque
```

O desafio 02 sem argumento usa o `estoque.txt` da própria pasta; para os dois, o caminho
do arquivo de entrada também pode ser passado como argumento. O relatório é exibido no
console e salvo em `.txt` dentro da pasta do desafio (ex.:
`01-comissao-vendas/relatorio-comissao.txt` e `02-movimentacoes-estoque/relatorio-estoque.txt`).

## Estrutura

```
target/
├── .gitignore
├── README.md
├── 01-comissao-vendas/
│   ├── Program.cs
│   ├── ComissaoVendas.csproj
│   ├── README.md
│   ├── vendas.txt
│   └── relatorio-comissao.txt   (gerado a cada execução)
└── 02-movimentacoes-estoque/
    ├── Program.cs
    ├── MovimentacoesEstoque.csproj
    ├── README.md
    ├── estoque.txt
    └── relatorio-estoque.txt    (gerado a cada execução)
```
