namespace CurrencyConverter.Controllers.Models
{
	public record ConversionResultDto(string FromCurrencyCode, string ToCurrencyCode, decimal OriginalAmount, decimal ConvertedAmount, decimal Rate, DateTime Timestamp);
}