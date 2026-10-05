# desafio_target

Desafios técnicos da **Target** escritos em **C# / .NET 8**, um por pasta.

## Desafios

| # | Pasta | Assunto | Como rodar |
|---|---|---|---|
| 01 | [`01-comissao-vendas`](01-comissao-vendas/) | Cálculo de comissão de vendas a partir de JSON | `dotnet run --project 01-comissao-vendas -- 01-comissao-vendas/vendas.txt` |

Cada pasta contém o seu `Program.cs`, o `.csproj`, o arquivo de dados de entrada e um
`README.md` explicando a solução em detalhes.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Como executar

```bash
dotnet run --project 01-comissao-vendas -- 01-comissao-vendas/vendas.txt
```

O relatório é exibido no console e também salvo em `.txt` dentro da pasta do desafio
(ex.: `01-comissao-vendas/relatorio-comissao.txt`).

## Estrutura

```
target/
├── .gitignore
├── README.md
└── 01-comissao-vendas/
    ├── Program.cs
    ├── ComissaoVendas.csproj
    ├── README.md
    ├── vendas.txt
    └── relatorio-comissao.txt   (gerado a cada execução)
```
