using System.Globalization;

var brazilianCulture = CultureInfo.GetCultureInfo("pt-BR");
var value = ReadPositiveDecimal("Valor devido: ", brazilianCulture);
var dueDate = ReadDate("Data de vencimento (dd/MM/aaaa): ", brazilianCulture);

var overdueDays = Math.Max(0, (DateOnly.FromDateTime(DateTime.Today).DayNumber - dueDate.DayNumber));
var interest = value * 0.025m * overdueDays;
var total = value + interest;

Console.WriteLine();
Console.WriteLine($"Dias em atraso: {overdueDays}");
Console.WriteLine($"Juros: {interest.ToString("C", brazilianCulture)}");
Console.WriteLine($"Valor total: {total.ToString("C", brazilianCulture)}");

static decimal ReadPositiveDecimal(string prompt, CultureInfo culture)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();

        if (decimal.TryParse(input, NumberStyles.Number, culture, out var value) && value >= 0)
        {
            return value;
        }

        Console.WriteLine("Informe um valor válido, usando vírgula como separador decimal.");
    }
}

static DateOnly ReadDate(string prompt, CultureInfo culture)
{
    while (true)
    {
        Console.Write(prompt);
        var input = Console.ReadLine();

        if (DateOnly.TryParseExact(input, "dd/MM/yyyy", culture, DateTimeStyles.None, out var dueDate))
        {
            return dueDate;
        }

        Console.WriteLine("Informe a data no formato dd/MM/aaaa.");
    }
}
