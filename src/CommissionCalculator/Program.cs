using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

var dataPath = Path.Combine(AppContext.BaseDirectory, "SalesData.json");
var json = await File.ReadAllTextAsync(dataPath);
var data = JsonSerializer.Deserialize<SalesData>(json)
    ?? throw new InvalidOperationException("Não foi possível ler os dados de vendas.");

var brazilianCulture = CultureInfo.GetCultureInfo("pt-BR");

Console.WriteLine("Comissões por vendedor");
Console.WriteLine();

foreach (var result in data.Sales
             .GroupBy(sale => sale.Seller)
             .OrderBy(group => group.Key, StringComparer.CurrentCulture))
{
    var commission = result.Sum(sale => CalculateCommission(sale.Value));
    Console.WriteLine($"{result.Key}: {commission.ToString("C", brazilianCulture)}");
}

static decimal CalculateCommission(decimal saleValue) => saleValue switch
{
    < 100m => 0m,
    < 500m => saleValue * 0.01m,
    _ => saleValue * 0.05m
};

internal sealed class SalesData
{
    [JsonPropertyName("vendas")]
    public List<Sale> Sales { get; init; } = [];
}

internal sealed class Sale
{
    [JsonPropertyName("vendedor")]
    public string Seller { get; init; } = string.Empty;

    [JsonPropertyName("valor")]
    public decimal Value { get; init; }
}
