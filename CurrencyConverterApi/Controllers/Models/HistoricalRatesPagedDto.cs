namespace CurrencyConverter.Controllers.Models
{
	public class HistoricalRatesPagedDto
	{
		public required string CurrencyCode { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
		public required Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; }
		public string? NextCursor { get; set; }
	}
}