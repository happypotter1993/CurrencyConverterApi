
namespace CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter
{
	public interface IFrankfurterClient
	{
		HashSet<string> SupportedCurrencyCodes { get; }

		Task<LatestRatesResponse?> GetLatestRatesAsync(string currencyCode = "EUR", IEnumerable<string>? symbols = null);
		Task<Dictionary<string, string>> GetSupportedCurrenciesAsync();
		Task<TimeSeriesResponse?> GetTimeSeriesAsync(DateTime startDate, DateTime endDate, string currencyCode = "EUR", IEnumerable<string>? symbols = null);
	}
}