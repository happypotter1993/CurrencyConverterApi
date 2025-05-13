namespace CurrencyConverter.Services.Models;

public class PaginatedHistoricalRates
{
	public Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; } = [];
	public int Page { get; set; }
	public int PageSize { get; set; }
	public int TotalCount { get; set; }
}
