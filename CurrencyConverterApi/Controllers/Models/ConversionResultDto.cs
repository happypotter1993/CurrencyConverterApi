public class ConversionResultDto
{
	public required string FromCurrencyCode { get; set; }
	public required string ToCurrencyCode { get; set; }
	public decimal OriginalAmount { get; set; }
	public decimal ConvertedAmount { get; set; }
	public decimal Rate { get; set; }
	public DateTime Timestamp { get; set; }
}
