using System.Globalization;
using System.Text;

namespace JurosAtraso;

internal static class Program
{
    private const decimal TaxaDiaria = 0.025m;
    private const int LarguraDoRotulo = 22;
    private const int LarguraDoValor = 14;
    private const string NomeDoRelatorio = "relatorio-juros.txt";

    private static readonly CultureInfo FormatoBrasileiro = CultureInfo.GetCultureInfo("pt-BR");
    private static readonly string[] FormatosDeData = ["dd/MM/yyyy", "yyyy-MM-dd"];

    private static int Main(string[] argumentos)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var entrada = LerEntrada(argumentos);
        if (entrada is null)
            return 1;

        try
        {
            var calculo = Calcular(entrada.Value.Valor, entrada.Value.Vencimento, DateTime.Today);
            var relatorio = GerarRelatorio(calculo);

            Console.Write(relatorio);

            var caminhoDoRelatorio = SalvarRelatorio(relatorio);
            Console.WriteLine($"Relatório salvo em: {caminhoDoRelatorio}");
            return 0;
        }
        catch (Exception erro) when (erro is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Erro: {erro.Message}");
            return 1;
        }
    }

    private static (decimal Valor, DateTime Vencimento)? LerEntrada(string[] argumentos) =>
        argumentos.Length > 0
            ? LerEntradaDosArgumentos(argumentos)
            : LerEntradaPeloConsole();

    private static (decimal Valor, DateTime Vencimento)? LerEntradaDosArgumentos(string[] argumentos)
    {
        if (argumentos.Length != 2)
        {
            Console.WriteLine("Uso: dotnet run -- <valor> <data de vencimento>");
            Console.WriteLine("Ex.: dotnet run -- 150,50 20/09/2026");
            return null;
        }

        var erroDoValor = ValidarValor(argumentos[0], out var valor);
        if (erroDoValor is not null)
        {
            Console.WriteLine(erroDoValor);
            return null;
        }

        var erroDaData = ValidarData(argumentos[1], out var vencimento);
        if (erroDaData is not null)
        {
            Console.WriteLine(erroDaData);
            return null;
        }

        return (valor, vencimento);
    }

    private static (decimal Valor, DateTime Vencimento)? LerEntradaPeloConsole()
    {
        Console.WriteLine("Informe os dados do título.\n");

        var valor = PedirValor();
        if (valor is null)
            return null;

        var vencimento = PedirVencimento();
        if (vencimento is null)
            return null;

        return (valor.Value, vencimento.Value);
    }

    private static decimal? PedirValor()
    {
        while (true)
        {
            Console.Write("Valor: ");
            var texto = Console.ReadLine();

            if (texto is null)
                return null;

            if (string.IsNullOrWhiteSpace(texto))
            {
                Console.WriteLine("Valor vazio. Tente novamente.");
                continue;
            }

            var erro = ValidarValor(texto, out var valor);
            if (erro is not null)
            {
                Console.WriteLine(erro);
                continue;
            }

            return valor;
        }
    }

    private static DateTime? PedirVencimento()
    {
        while (true)
        {
            Console.Write("Vencimento (dd/MM/aaaa): ");
            var texto = Console.ReadLine();

            if (texto is null)
                return null;

            if (string.IsNullOrWhiteSpace(texto))
            {
                Console.WriteLine("Data vazia. Tente novamente.");
                continue;
            }

            var erro = ValidarData(texto, out var vencimento);
            if (erro is not null)
            {
                Console.WriteLine(erro);
                continue;
            }

            return vencimento;
        }
    }

    private static string? ValidarValor(string texto, out decimal valor)
    {
        if (!TryConverterValor(texto, out valor))
            return $"Valor inválido: {texto}. Use algo como 150,50 ou 150.50.";

        if (valor <= 0)
            return "O valor deve ser maior que zero.";

        return null;
    }

    private static string? ValidarData(string texto, out DateTime data)
    {
        if (!TryConverterData(texto, out data))
            return $"Data de vencimento inválida: {texto}. Use dd/MM/aaaa ou aaaa-MM-dd.";

        return null;
    }

    private static bool TryConverterValor(string texto, out decimal valor)
    {
        var entrada = texto.Trim();

        return entrada.Contains(',')
            ? decimal.TryParse(entrada, NumberStyles.Number, FormatoBrasileiro, out valor)
            : decimal.TryParse(entrada, NumberStyles.Number, CultureInfo.InvariantCulture, out valor);
    }

    private static bool TryConverterData(string texto, out DateTime data) =>
        DateTime.TryParseExact(texto.Trim(), FormatosDeData, CultureInfo.InvariantCulture, DateTimeStyles.None, out data);

    private static CalculoDeJuros Calcular(decimal valor, DateTime vencimento, DateTime hoje)
    {
        var diasEmAtraso = Math.Max((hoje - vencimento).Days, 0);
        return new CalculoDeJuros(valor, vencimento, hoje, diasEmAtraso, valor * TaxaDiaria * diasEmAtraso);
    }

    private static string GerarRelatorio(CalculoDeJuros calculo)
    {
        using var saida = new StringWriter();

        saida.WriteLine("CÁLCULO DE JUROS POR ATRASO");
        saida.WriteLine(new string('-', LarguraDoRotulo + LarguraDoValor));
        saida.WriteLine();
        EscreverCampo(saida, "Valor original", Moeda(calculo.ValorOriginal));
        EscreverCampo(saida, "Vencimento", Data(calculo.Vencimento));
        EscreverCampo(saida, "Data do cálculo", Data(calculo.DataDoCalculo));
        EscreverCampo(saida, "Dias em atraso", calculo.DiasEmAtraso.ToString("N0", FormatoBrasileiro));
        EscreverCampo(saida, "Multa (2,5% ao dia)", Moeda(calculo.Juros));
        EscreverCampo(saida, "Total a pagar", Moeda(calculo.Total));

        return saida.ToString();
    }

    private static void EscreverCampo(TextWriter saida, string rotulo, string valor) =>
        saida.WriteLine($"{rotulo.PadRight(LarguraDoRotulo)}{valor.PadLeft(LarguraDoValor)}");

    private static string Moeda(decimal valor) => $"R$ {valor.ToString("N2", FormatoBrasileiro)}";

    private static string Data(DateTime data) => data.ToString("dd/MM/yyyy", FormatoBrasileiro);

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

internal sealed record CalculoDeJuros(
    decimal ValorOriginal,
    DateTime Vencimento,
    DateTime DataDoCalculo,
    int DiasEmAtraso,
    decimal Juros)
{
    public decimal Total => ValorOriginal + Juros;
}
