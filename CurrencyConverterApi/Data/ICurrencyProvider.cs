using CurrencyConverter.Services.Models;

using CurrencyCode = Frankfurter.API.Client.Domain.CurrencyCode;

namespace CurrencyConverter.Data;

public interface ICurrencyProvider
{
	Task<Dictionary<CurrencyCode, decimal>> GetLatestRatesAsync(CurrencyCode baseCurrency);
	Task<decimal> ConvertAsync(CurrencyCode from, CurrencyCode to, decimal amount);
	Task<PaginatedHistoricalRates> GetHistoricalRatesAsync(
		CurrencyCode baseCurrency,
		List<CurrencyCode> toCurrencies,
		DateTime from,
		DateTime to,
		int page = 1,
		int pageSize = 100);
}
