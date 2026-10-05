using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MovimentacoesEstoque;

internal static class Program
{
    private const int LarguraId = 4;
    private const int LarguraDescricaoDaMovimentacao = 32;
    private const int LarguraProduto = 30;
    private const int LarguraQuantidadeDaMovimentacao = 8;
    private const int LarguraSaldoApos = 14;

    private const int LarguraCodigo = 8;
    private const int LarguraTotalizador = 14;
    private const int LarguraSaida = 10;
    private const int LarguraSaldoFinal = 12;

    private const string SeparadorDeColuna = "  ";
    private const string NomeDoArquivoDeEntrada = "estoque.txt";
    private const string NomeDoRelatorio = "relatorio-estoque.txt";

    private static readonly CultureInfo FormatoBrasileiro = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly JsonSerializerOptions OpcoesDoJson = new(JsonSerializerDefaults.Web);

    private static int Main(string[] argumentos)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var caminho = ObterCaminhoDoArquivo(argumentos);
        if (caminho is null)
            return 1;

        try
        {
            var documento = LerDocumento(caminho);
            var movimentacoes = PrepararMovimentacoes(documento.Movimentacoes);
            var resultado = Processar(documento.Estoque!, movimentacoes);

            var relatorio = GerarRelatorio(resultado);
            Console.Write(relatorio);

            var caminhoDoRelatorio = SalvarRelatorio(relatorio);
            Console.WriteLine($"Relatório salvo em: {caminhoDoRelatorio}");
            return 0;
        }
        catch (Exception erro) when (
            erro is IOException
                or UnauthorizedAccessException
                or JsonException
                or DadosDeEstoqueInvalidosException)
        {
            Console.WriteLine($"Erro: {erro.Message}");
            return 1;
        }
    }

    private static string? ObterCaminhoDoArquivo(string[] argumentos)
    {
        var caminho = argumentos.Length > 0
            ? NormalizarCaminho(argumentos[0])
            : CaminhoPadrao();

        if (File.Exists(caminho))
            return caminho;

        Console.WriteLine($"Arquivo não encontrado: {caminho}");
        return null;
    }

    private static string CaminhoPadrao() =>
        Path.Combine(ObterPastaDoProjeto(), NomeDoArquivoDeEntrada);

    private static string NormalizarCaminho(string caminho) => caminho.Trim().Trim('"');

    private static Documento LerDocumento(string caminho)
    {
        var json = File.ReadAllText(caminho);
        var documento = JsonSerializer.Deserialize<Documento>(json, OpcoesDoJson);

        if (documento?.Estoque is null || documento.Estoque.Count == 0)
            throw new DadosDeEstoqueInvalidosException("O arquivo não contém produtos no estoque.");

        foreach (var produto in documento.Estoque)
            ValidarProduto(produto);

        var codigos = documento.Estoque.Select(produto => produto.CodigoProduto).ToList();
        if (codigos.Distinct().Count() != codigos.Count)
            throw new DadosDeEstoqueInvalidosException("Existem produtos com código duplicado.");

        return documento;
    }

    private static void ValidarProduto(Produto produto)
    {
        if (string.IsNullOrWhiteSpace(produto.DescricaoProduto))
            throw new DadosDeEstoqueInvalidosException($"O produto {produto.CodigoProduto} não tem descrição.");
    }

    private static IReadOnlyList<Movimentacao> PrepararMovimentacoes(IReadOnlyList<MovimentacaoBruta>? informadas)
    {
        var lista = informadas ?? [];
        var movimentacoes = new List<Movimentacao>(lista.Count);

        for (var indice = 0; indice < lista.Count; indice++)
        {
            var bruta = lista[indice];
            var referencia = $"Movimentação {indice + 1}";

            if (string.IsNullOrWhiteSpace(bruta.Descricao))
                throw new DadosDeEstoqueInvalidosException(
                    $"{referencia}: informe a descrição da movimentação (ENTRADA ou SAIDA).");

            if (bruta.Quantidade <= 0)
                throw new DadosDeEstoqueInvalidosException($"{referencia}: a quantidade deve ser maior que zero.");

            var tipo = ConverterTipo(bruta.Descricao, referencia);
            movimentacoes.Add(new Movimentacao(movimentacoes.Count + 1, tipo, bruta.CodigoProduto, bruta.Quantidade));
        }

        return movimentacoes;
    }

    private static TipoMovimentacao ConverterTipo(string descricao, string referencia) =>
        descricao.Trim().ToUpperInvariant() switch
        {
            "ENTRADA" => TipoMovimentacao.Entrada,
            "SAIDA" or "SAÍDA" => TipoMovimentacao.Saida,
            _ => throw new DadosDeEstoqueInvalidosException(
                $"{referencia}: \"{descricao}\" não identifica a movimentação. Use ENTRADA ou SAIDA.")
        };

    private static ResultadoProcessamento Processar(IReadOnlyList<Produto> produtos, IReadOnlyList<Movimentacao> movimentacoes)
    {
        var produtoPorCodigo = produtos.ToDictionary(produto => produto.CodigoProduto);
        var saldoPorCodigo = produtos.ToDictionary(produto => produto.CodigoProduto, produto => produto.Estoque);
        var entradasPorCodigo = new Dictionary<int, int>();
        var saidasPorCodigo = new Dictionary<int, int>();
        var linhas = new List<ResultadoMovimentacao>(movimentacoes.Count);

        foreach (var movimentacao in movimentacoes)
        {
            if (!produtoPorCodigo.TryGetValue(movimentacao.CodigoProduto, out var produto))
                throw new DadosDeEstoqueInvalidosException(
                    $"Movimentação {movimentacao.Id}: produto com código {movimentacao.CodigoProduto} não encontrado.");

            var saldo = saldoPorCodigo[movimentacao.CodigoProduto];
            var saldoApos = movimentacao.Tipo == TipoMovimentacao.Entrada
                ? saldo + movimentacao.Quantidade
                : saldo - movimentacao.Quantidade;

            if (saldoApos < 0)
                throw new DadosDeEstoqueInvalidosException(
                    $"Movimentação {movimentacao.Id}: saída de {movimentacao.Quantidade} un. de {produto.DescricaoProduto} " +
                    $"excede o estoque disponível ({saldo} un.).");

            saldoPorCodigo[produto.CodigoProduto] = saldoApos;

            if (movimentacao.Tipo == TipoMovimentacao.Entrada)
                entradasPorCodigo[produto.CodigoProduto] = entradasPorCodigo.GetValueOrDefault(produto.CodigoProduto) + movimentacao.Quantidade;
            else
                saidasPorCodigo[produto.CodigoProduto] = saidasPorCodigo.GetValueOrDefault(produto.CodigoProduto) + movimentacao.Quantidade;

            linhas.Add(new ResultadoMovimentacao(movimentacao, produto, saldoApos));
        }

        var estoqueFinal = produtos
            .Select(produto => new ResumoProduto(
                produto.CodigoProduto,
                produto.DescricaoProduto,
                produto.Estoque,
                entradasPorCodigo.GetValueOrDefault(produto.CodigoProduto),
                saidasPorCodigo.GetValueOrDefault(produto.CodigoProduto),
                saldoPorCodigo[produto.CodigoProduto]))
            .ToList();

        return new ResultadoProcessamento(linhas, estoqueFinal);
    }

    private static string GerarRelatorio(ResultadoProcessamento resultado)
    {
        using var saida = new StringWriter();
        EscreverMovimentacoes(saida, resultado.Movimentacoes);
        saida.WriteLine();
        EscreverEstoqueFinal(saida, resultado.EstoqueFinal);
        return saida.ToString();
    }

    private static void EscreverMovimentacoes(TextWriter saida, IReadOnlyList<ResultadoMovimentacao> movimentacoes)
    {
        if (movimentacoes.Count == 0)
        {
            saida.WriteLine("Nenhuma movimentação informada.");
            return;
        }

        saida.WriteLine(Montar(
            "Id".PadLeft(LarguraId),
            "Descrição".PadRight(LarguraDescricaoDaMovimentacao),
            "Produto".PadRight(LarguraProduto),
            "Qtd".PadLeft(LarguraQuantidadeDaMovimentacao),
            "Saldo após".PadLeft(LarguraSaldoApos)));

        saida.WriteLine(LinhaSeparadora(
            LarguraId,
            LarguraDescricaoDaMovimentacao,
            LarguraProduto,
            LarguraQuantidadeDaMovimentacao,
            LarguraSaldoApos));

        foreach (var movimentacao in movimentacoes)
            EscreverLinhaDaMovimentacao(saida, movimentacao);
    }

    private static void EscreverLinhaDaMovimentacao(TextWriter saida, ResultadoMovimentacao resultado)
    {
        var movimentacao = resultado.Movimentacao;
        var sinal = movimentacao.Tipo == TipoMovimentacao.Entrada ? "+" : "-";

        saida.WriteLine(Montar(
            movimentacao.Id.ToString().PadLeft(LarguraId),
            movimentacao.Descricao.PadRight(LarguraDescricaoDaMovimentacao),
            NomeDoProduto(resultado.Produto).PadRight(LarguraProduto),
            (sinal + Quantidade(movimentacao.Quantidade)).PadLeft(LarguraQuantidadeDaMovimentacao),
            Quantidade(resultado.SaldoApos).PadLeft(LarguraSaldoApos)));
    }

    private static void EscreverEstoqueFinal(TextWriter saida, IReadOnlyList<ResumoProduto> estoque)
    {
        saida.WriteLine(Montar(
            "Código".PadLeft(LarguraCodigo),
            "Produto".PadRight(LarguraProduto),
            "Inicial".PadLeft(LarguraTotalizador),
            "Entradas".PadLeft(LarguraTotalizador),
            "Saídas".PadLeft(LarguraSaida),
            "Saldo final".PadLeft(LarguraSaldoFinal)));

        saida.WriteLine(LinhaSeparadora(
            LarguraCodigo,
            LarguraProduto,
            LarguraTotalizador,
            LarguraTotalizador,
            LarguraSaida,
            LarguraSaldoFinal));

        foreach (var produto in estoque)
            EscreverLinhaDoProduto(saida, produto);

        saida.WriteLine(Montar(
            string.Empty.PadLeft(LarguraCodigo),
            "TOTAL".PadRight(LarguraProduto),
            Quantidade(estoque.Sum(produto => produto.EstoqueInicial)).PadLeft(LarguraTotalizador),
            Quantidade(estoque.Sum(produto => produto.Entradas)).PadLeft(LarguraTotalizador),
            Quantidade(estoque.Sum(produto => produto.Saidas)).PadLeft(LarguraSaida),
            Quantidade(estoque.Sum(produto => produto.SaldoFinal)).PadLeft(LarguraSaldoFinal)));
    }

    private static void EscreverLinhaDoProduto(TextWriter saida, ResumoProduto produto) =>
        saida.WriteLine(Montar(
            produto.CodigoProduto.ToString().PadLeft(LarguraCodigo),
            produto.DescricaoProduto.PadRight(LarguraProduto),
            Quantidade(produto.EstoqueInicial).PadLeft(LarguraTotalizador),
            Quantidade(produto.Entradas).PadLeft(LarguraTotalizador),
            Quantidade(produto.Saidas).PadLeft(LarguraSaida),
            Quantidade(produto.SaldoFinal).PadLeft(LarguraSaldoFinal)));

    private static string Montar(params string[] colunas) =>
        string.Join(SeparadorDeColuna, colunas);

    private static string LinhaSeparadora(params int[] larguras) =>
        string.Join(SeparadorDeColuna, larguras.Select(largura => new string('-', largura)));

    private static string NomeDoProduto(Produto produto) => $"{produto.DescricaoProduto} ({produto.CodigoProduto})";

    private static string Quantidade(int valor) => valor.ToString("N0", FormatoBrasileiro);

    private static string SalvarRelatorio(string conteudo)
    {
        var caminho = Path.Combine(ObterPastaDoProjeto(), NomeDoRelatorio);
        File.WriteAllText(caminho, conteudo);
        return caminho;
    }

    private static string ObterPastaDoProjeto()
    {
        for (var pasta = new DirectoryInfo(AppContext.BaseDirectory); pasta is not null; pasta = pasta.Parent)
        {
            if (pasta.EnumerateFiles("*.csproj").Any())
                return pasta.FullName;
        }

        return AppContext.BaseDirectory;
    }
}

internal sealed record Documento(List<Produto>? Estoque, List<MovimentacaoBruta>? Movimentacoes);

internal sealed record Produto(int CodigoProduto, string DescricaoProduto, int Estoque);

internal sealed record MovimentacaoBruta(string? Descricao, int CodigoProduto, int Quantidade);

internal enum TipoMovimentacao
{
    Entrada,
    Saida
}

internal sealed record Movimentacao(int Id, TipoMovimentacao Tipo, int CodigoProduto, int Quantidade)
{
    public string Descricao => Tipo == TipoMovimentacao.Entrada ? "Entrada" : "Saída";
}

internal sealed record ResultadoMovimentacao(Movimentacao Movimentacao, Produto Produto, int SaldoApos);

internal sealed record ResumoProduto(
    int CodigoProduto,
    string DescricaoProduto,
    int EstoqueInicial,
    int Entradas,
    int Saidas,
    int SaldoFinal);

internal sealed record ResultadoProcessamento(
    IReadOnlyList<ResultadoMovimentacao> Movimentacoes,
    IReadOnlyList<ResumoProduto> EstoqueFinal);

internal sealed class DadosDeEstoqueInvalidosException(string mensagem) : Exception(mensagem);
