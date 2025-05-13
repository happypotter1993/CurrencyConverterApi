using CurrencyConverter.Controllers.Models;
using CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;

namespace CurrencyConverter.Services
{
	public interface ICurrencyExchangeRateService
	{
		Task<ConversionResultDto> ConvertAsync(string fromCurrencyCode, string toCurrencyCode, decimal amount);
		Task<HistoricalRatesPagedDto> GetHistoricalRatesAsync(DateTime startDate, DateTime endDate, string currencyCode = "EUR", int pageNumber = 1, int pageSize = 10);
		Task<LatestRatesResponse?> GetLatestRatesAsync(string currencyCode = "EUR");
		Task<IEnumerable<SupportedCurrencyDto>> GetSupportedCurrenciesAsync();
	}
}