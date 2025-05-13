namespace CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;

public class FrankfurterClient : IFrankfurterClient
{
	private readonly HttpClient _frankfurtHttpClient;

	public FrankfurterClient(HttpClient http)
	{
		_frankfurtHttpClient = http;
		if (_frankfurtHttpClient.BaseAddress == null)
			_frankfurtHttpClient.BaseAddress = new Uri("https://api.frankfurter.dev/");

		Dictionary<string, string>? supportedCurrencies = _frankfurtHttpClient
			.GetFromJsonAsync<Dictionary<string, string>>("v1/currencies")
			.GetAwaiter()
			.GetResult();

		SupportedCurrencyCodes = supportedCurrencies?
			.Keys
			.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
	}

	public HashSet<string> SupportedCurrencyCodes { get; private set; }

	private void ValidateCurrencyCodes(params string[] currencyCodes)
	{
		foreach (var currencyCode in currencyCodes)
		{
			if (!SupportedCurrencyCodes.Contains(currencyCode))
				throw new ArgumentException($"Unsupported currency: {currencyCode}");
		}
	}

	public async Task<Dictionary<string, string>> GetSupportedCurrenciesAsync()
	{
		Dictionary<string, string> supportedCurrencies = await _frankfurtHttpClient.GetFromJsonAsync<Dictionary<string, string>?>("v1/currencies")??[];

		if (supportedCurrencies == null || supportedCurrencies.Count == 0)
			return [];

		SupportedCurrencyCodes = supportedCurrencies
			.Keys
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		return supportedCurrencies;
	}

	public async Task<LatestRatesResponse?> GetLatestRatesAsync(
		string currencyCode = "EUR",
		IEnumerable<string>? symbols = null)
	{
		ValidateCurrencyCodes(currencyCode);
		if (symbols != null && symbols.Any())
			ValidateCurrencyCodes([.. symbols]);

		var url = $"v1/latest?base={currencyCode}";
		if (symbols != null && symbols.Any())
			url += "&symbols=" + string.Join(',', symbols);

		return await _frankfurtHttpClient
			.GetFromJsonAsync<LatestRatesResponse>(url);
	}

	public async Task<TimeSeriesResponse?> GetTimeSeriesAsync(
		DateTime startDate,
		DateTime endDate,
		string currencyCode = "EUR",
		IEnumerable<string>? symbols = null)
	{
		ValidateCurrencyCodes(currencyCode);
		if (symbols != null && symbols.Any())
			ValidateCurrencyCodes([.. symbols]);

		var url = $"v1/{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?base={currencyCode}";
		if (symbols != null && symbols.Any())
			url += "&symbols=" + string.Join(',', symbols);

		return await _frankfurtHttpClient.GetFromJsonAsync<TimeSeriesResponse>(url);
	}
}

public record LatestRatesResponse(
	string Base,
	string Date,
	Dictionary<string, decimal> Rates
);

public record TimeSeriesResponse(
	string Base,
	string StartDate,
	string EndDate,
	Dictionary<string, Dictionary<string, decimal>> Rates
);
