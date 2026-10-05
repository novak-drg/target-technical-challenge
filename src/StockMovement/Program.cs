using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

var dataPath = Path.Combine(AppContext.BaseDirectory, "InventoryData.json");
var json = await File.ReadAllTextAsync(dataPath);
var inventory = JsonSerializer.Deserialize<InventoryData>(json)
    ?? throw new InvalidOperationException("Não foi possível ler os dados do estoque.");

var movementService = new InventoryService(inventory.Products);

Console.WriteLine("Movimentação de estoque");
Console.WriteLine("Digite 0 como código do produto para encerrar.");

while (true)
{
    Console.WriteLine();
    PrintProducts(inventory.Products);

    var productCode = ReadPositiveInteger("Código do produto (0 para encerrar): ", allowZero: true);
    if (productCode == 0)
    {
        break;
    }

    var movementType = ReadMovementType();
    var quantity = ReadPositiveInteger("Quantidade: ");
    var description = ReadRequiredText("Descrição da movimentação: ");

    var result = movementService.RegisterMovement(productCode, movementType, quantity, description);

    if (!result.IsSuccess)
    {
        Console.WriteLine($"Não foi possível registrar a movimentação: {result.ErrorMessage}");
        continue;
    }

    Console.WriteLine();
    Console.WriteLine($"Movimentação registrada. Identificador: {result.Movement!.Id}");
    Console.WriteLine($"Produto: {result.Product!.Description}");
    Console.WriteLine($"Estoque final: {result.Product.Stock}");
}

Console.WriteLine("Programa encerrado.");

static void PrintProducts(IEnumerable<Product> products)
{
    Console.WriteLine("\nProdutos disponíveis:");
    foreach (var product in products.OrderBy(product => product.ProductCode))
    {
        Console.WriteLine($"{product.ProductCode} - {product.Description} (estoque: {product.Stock})");
    }
}

static int ReadPositiveInteger(string prompt, bool allowZero = false)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();

        if (int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && (value > 0 || allowZero && value == 0))
        {
            return value;
        }

        Console.WriteLine(allowZero
            ? "Informe um número inteiro maior ou igual a zero."
            : "Informe um número inteiro maior que zero.");
    }
}

static MovementType ReadMovementType()
{
    while (true)
    {
        Console.Write("Tipo da movimentação (E para entrada ou S para saída): ");
        var input = Console.ReadLine()?.Trim().ToUpperInvariant();

        if (input == "E")
        {
            return MovementType.Entry;
        }

        if (input == "S")
        {
            return MovementType.Exit;
        }

        Console.WriteLine("Informe E para entrada ou S para saída.");
    }
}

static string ReadRequiredText(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine()?.Trim();

        if (!string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        Console.WriteLine("A descrição é obrigatória.");
    }
}

internal sealed class InventoryData
{
    [JsonPropertyName("estoque")]
    public List<Product> Products { get; init; } = [];
}

internal sealed class Product
{
    [JsonPropertyName("codigoProduto")]
    public int ProductCode { get; init; }

    [JsonPropertyName("descricaoProduto")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("estoque")]
    public int Stock { get; set; }
}

internal sealed record StockMovement(long Id, int ProductCode, MovementType Type, int Quantity, string Description);

internal enum MovementType
{
    Entry,
    Exit
}

internal sealed class InventoryService(List<Product> products)
{
    private static long _lastMovementId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000;

    public MovementRegistrationResult RegisterMovement(
        int productCode,
        MovementType movementType,
        int quantity,
        string description)
    {
        var product = products.SingleOrDefault(item => item.ProductCode == productCode);
        if (product is null)
        {
            return MovementRegistrationResult.Failure("Produto não encontrado.");
        }

        if (movementType == MovementType.Exit && quantity > product.Stock)
        {
            return MovementRegistrationResult.Failure("A saída não pode ser maior que o estoque disponível.");
        }

        product.Stock += movementType == MovementType.Entry ? quantity : -quantity;

        var movement = new StockMovement(
            Interlocked.Increment(ref _lastMovementId),
            product.ProductCode,
            movementType,
            quantity,
            description);

        return MovementRegistrationResult.Success(product, movement);
    }
}

internal sealed record MovementRegistrationResult(Product? Product, StockMovement? Movement, string? ErrorMessage)
{
    public bool IsSuccess => ErrorMessage is null;

    public static MovementRegistrationResult Success(Product product, StockMovement movement) =>
        new(product, movement, null);

    public static MovementRegistrationResult Failure(string message) =>
        new(null, null, message);
}
