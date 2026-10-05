using System.Globalization;
using System.Text.Json;

namespace ComissaoVendas;

internal static class Program
{
    private const decimal LimiteDeIsencao = 100m;
    private const decimal LimiteDeComissaoPadrao = 500m;
    private const decimal PercentualPadrao = 0.01m;
    private const decimal PercentualMaximo = 0.05m;

    private const int LarguraVendedor = 20;
    private const int LarguraQuantidade = 6;
    private const int LarguraValor = 18;

    private const string NomeDoRelatorio = "relatorio-comissao.txt";

    private static readonly CultureInfo FormatoBrasileiro = CultureInfo.GetCultureInfo("pt-BR");

    private static int Main(string[] argumentos)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var caminho = ObterCaminhoDoArquivo(argumentos);
        if (caminho is null)
            return 1;

        try
        {
            var vendas = LerVendas(caminho);
            var resumos = CalcularComissoesPorVendedor(vendas);
            var relatorio = GerarRelatorio(resumos);

            Console.Write(relatorio);

            var caminhoDoRelatorio = SalvarRelatorio(relatorio, caminho);
            Console.WriteLine($"Relatório salvo em: {caminhoDoRelatorio}");
            return 0;
        }
        catch (Exception erro) when (
            erro is IOException
                or UnauthorizedAccessException
                or JsonException
                or DadosDeVendaInvalidosException)
        {
            Console.WriteLine($"Erro: {erro.Message}");
            return 1;
        }
    }

    private static string? ObterCaminhoDoArquivo(string[] argumentos)
    {
        if (argumentos.Length > 0)
        {
            var informado = NormalizarCaminho(argumentos[0]);
            if (File.Exists(informado))
                return informado;

            Console.WriteLine($"Arquivo não encontrado: {informado}");
        }

        return PedirCaminhoAoUsuario();
    }

    private static string? PedirCaminhoAoUsuario()
    {
        while (true)
        {
            Console.Write("Informe o caminho do arquivo de vendas (ou arraste o arquivo aqui): ");
            var entrada = Console.ReadLine();

            if (entrada is null)
                return null;

            if (string.IsNullOrWhiteSpace(entrada))
            {
                Console.WriteLine("Caminho vazio. Tente novamente.");
                continue;
            }

            var caminho = NormalizarCaminho(entrada);
            if (File.Exists(caminho))
                return caminho;

            Console.WriteLine("Arquivo não encontrado. Tente novamente.");
        }
    }

    private static string NormalizarCaminho(string caminho) => caminho.Trim().Trim('"');

    private static IReadOnlyList<Venda> LerVendas(string caminho)
    {
        var json = File.ReadAllText(caminho);
        var opcoes = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var documento = JsonSerializer.Deserialize<DocumentoDeVendas>(json, opcoes);

        if (documento?.Vendas is null || documento.Vendas.Count == 0)
            throw new DadosDeVendaInvalidosException("O arquivo não contém vendas.");

        foreach (var venda in documento.Vendas)
            Validar(venda);

        return documento.Vendas;
    }

    private static void Validar(Venda venda)
    {
        if (string.IsNullOrWhiteSpace(venda.Vendedor))
            throw new DadosDeVendaInvalidosException("Existe uma venda sem vendedor.");

        if (venda.Valor < 0)
            throw new DadosDeVendaInvalidosException($"Valor inválido para {venda.Vendedor}: {venda.Valor}.");
    }

    private static decimal CalcularComissao(Venda venda) => venda.Valor switch
    {
        < LimiteDeIsencao => 0m,
        < LimiteDeComissaoPadrao => venda.Valor * PercentualPadrao,
        _ => venda.Valor * PercentualMaximo
    };

    private static IReadOnlyList<ResumoVendedor> CalcularComissoesPorVendedor(IEnumerable<Venda> vendas) =>
        vendas
            .GroupBy(venda => venda.Vendedor, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => CriarResumo(grupo.Key, grupo.ToList()))
            .OrderByDescending(resumo => resumo.TotalComissao)
            .ToList();

    private static ResumoVendedor CriarResumo(string vendedor, List<Venda> vendasDoVendedor) =>
        new(
            vendedor,
            vendasDoVendedor.Count,
            vendasDoVendedor.Sum(venda => venda.Valor),
            vendasDoVendedor.Sum(CalcularComissao));

    private static string GerarRelatorio(IReadOnlyList<ResumoVendedor> resumos)
    {
        using var buffer = new StringWriter();
        EscreverRelatorio(buffer, resumos);
        return buffer.ToString();
    }

    private static string SalvarRelatorio(string conteudo, string caminhoDeEntrada)
    {
        var caminho = Path.Combine(ObterPastaDoProjeto(caminhoDeEntrada), NomeDoRelatorio);
        File.WriteAllText(caminho, conteudo);
        return caminho;
    }

    private static string ObterPastaDoProjeto(string caminhoDeEntrada)
    {
        for (var pasta = new DirectoryInfo(AppContext.BaseDirectory); pasta is not null; pasta = pasta.Parent)
        {
            if (pasta.EnumerateFiles("*.csproj").Any())
                return pasta.FullName;
        }

        return Path.GetDirectoryName(caminhoDeEntrada) is { Length: > 0 } pastaDeEntrada
            ? pastaDeEntrada
            : AppContext.BaseDirectory;
    }

    private static void EscreverRelatorio(TextWriter saida, IReadOnlyList<ResumoVendedor> resumos)
    {
        saida.WriteLine(
            $"{"Vendedor".PadRight(LarguraVendedor)}" +
            $"{"Qtd".PadLeft(LarguraQuantidade)}" +
            $"{"Total vendido".PadLeft(LarguraValor)}" +
            $"{"Comissão".PadLeft(LarguraValor)}");

        saida.WriteLine(new string('-', LarguraVendedor + LarguraQuantidade + 2 * LarguraValor));

        foreach (var resumo in resumos)
            EscreverLinha(saida, resumo);

        var total = new ResumoVendedor(
            "TOTAL",
            resumos.Sum(resumo => resumo.QuantidadeVendas),
            resumos.Sum(resumo => resumo.TotalVendido),
            resumos.Sum(resumo => resumo.TotalComissao));

        EscreverLinha(saida, total);
    }

    private static void EscreverLinha(TextWriter saida, ResumoVendedor resumo)
    {
        saida.WriteLine(
            $"{resumo.Vendedor.PadRight(LarguraVendedor)}" +
            $"{resumo.QuantidadeVendas.ToString().PadLeft(LarguraQuantidade)}" +
            $"{Moeda(resumo.TotalVendido).PadLeft(LarguraValor)}" +
            $"{Moeda(resumo.TotalComissao).PadLeft(LarguraValor)}");
    }

    private static string Moeda(decimal valor) => $"R$ {valor.ToString("N2", FormatoBrasileiro)}";
}

internal sealed record Venda(string Vendedor, decimal Valor);

internal sealed record ResumoVendedor(
    string Vendedor,
    int QuantidadeVendas,
    decimal TotalVendido,
    decimal TotalComissao);

internal sealed record DocumentoDeVendas(List<Venda>? Vendas);

internal sealed class DadosDeVendaInvalidosException(string mensagem) : Exception(mensagem);
