namespace CurrencyConverter.Controllers.Models
{
	public record LatestRatesDto(string CurrencyCode, DateTime Date, IDictionary<string, decimal> Rates);
}