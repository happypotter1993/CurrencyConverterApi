public class LatestRatesDto
{
	public required string CurrencyCode { get; set; }
	public DateTime Date { get; set; }
	public required IDictionary<string, decimal> Rates { get; set; }
}
