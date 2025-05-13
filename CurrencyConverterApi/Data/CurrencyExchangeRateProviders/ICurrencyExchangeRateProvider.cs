using CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;

namespace CurrencyConverter.Data.CurrencyExchangeRateProviders
{
	public interface ICurrencyExchangeRateProvider
	{
		Task<LatestRatesResponse?> GetLatestRatesAsync(string currencyCode, IEnumerable<string>? symbols = null);
		
		//Name to Currency Code Mapping
		Task<Dictionary<string, string>?> GetSupportedCurrenciesAsync();
		Task<TimeSeriesResponse?> GetTimeSeriesAsync(DateTime startDate, DateTime endDate, string currencyCode, IEnumerable<string>? symbols = null);
	}
}