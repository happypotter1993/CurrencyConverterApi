using CurrencyConverter.Controllers.Models;
using CurrencyConverter.Data.CurrencyExchangeRateProviders;
using CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;

namespace CurrencyConverter.Services;

public class CurrencyExchangeRateService(ICurrencyExchangeRateProvider provider) : ICurrencyExchangeRateService
{
	private readonly ICurrencyExchangeRateProvider _provider = provider;

	public async Task<IEnumerable<SupportedCurrencyDto>> GetSupportedCurrenciesAsync()
	{
		Dictionary<string, string> dict = await _provider.GetSupportedCurrenciesAsync()
								  ?? [];
		return dict.Select(kvp => new SupportedCurrencyDto(kvp.Key, kvp.Value));
	}

	public async Task<ConversionResultDto> ConvertAsync(
		string fromCurrencyCode,
		string toCurrencyCode,
		decimal amount)
	{
		LatestRatesResponse latest = await _provider.GetLatestRatesAsync(fromCurrencyCode, [toCurrencyCode])
						 ?? throw new InvalidOperationException("No rate data");
		if (!latest.Rates.TryGetValue(toCurrencyCode, out var rate))
			throw new InvalidOperationException($"Rate not found for {toCurrencyCode}");

		return new ConversionResultDto(fromCurrencyCode, toCurrencyCode, amount, Math.Round(amount * rate, 6), rate, DateTime.Parse(latest.Date));
	}

	public Task<LatestRatesResponse?> GetLatestRatesAsync(string currencyCode = "EUR")
		=> _provider.GetLatestRatesAsync(currencyCode);

	public async Task<HistoricalRatesPagedDto> GetHistoricalRatesAsync(
		DateTime startDate,
		DateTime endDate,
		string currencyCode = "EUR",
		int pageNumber = 1,
		int pageSize = 10)
	{
		TimeSeriesResponse full = await _provider.GetTimeSeriesAsync(startDate, endDate, currencyCode)
				  ?? throw new InvalidOperationException("No time-series data");

		// parse and sort dates
		var allDates = full.Rates
						   .Keys
						   .Select(d => DateTime.Parse(d))
						   .OrderBy(d => d)
						   .ToList();

		var totalPages = (int)Math.Ceiling(allDates.Count / (double)pageSize);

		var pageDates = allDates
			.Skip((pageNumber - 1) * pageSize)
			.Take(pageSize)
			.ToList();

		var pageRates = pageDates.ToDictionary(
			dt => dt,
			dt => full.Rates[dt.ToString("yyyy-MM-dd")]
		);

		var nextCursor = pageNumber < totalPages
			? $"/api/v1/exchange-rates/historical" +
			  $"?currencyCode={currencyCode}" +
			  $"&startDate={startDate:yyyy-MM-dd}" +
			  $"&endDate={endDate:yyyy-MM-dd}" +
			  $"&pageNumber={pageNumber + 1}" +
			  $"&pageSize={pageSize}"
			: null;

		return new HistoricalRatesPagedDto
		{
			CurrencyCode = currencyCode,
			StartDate = startDate,
			EndDate = endDate,
			PageNumber = pageNumber,
			PageSize = pageSize,
			TotalPages = totalPages,
			Rates = pageRates,
			NextCursor = nextCursor
		};
	}
}