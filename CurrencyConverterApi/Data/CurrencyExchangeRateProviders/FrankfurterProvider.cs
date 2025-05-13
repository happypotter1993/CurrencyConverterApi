using CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;

using Microsoft.Extensions.Caching.Memory;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

namespace CurrencyConverter.Data.CurrencyExchangeRateProviders;

public class FrankfurterProvider : ICurrencyExchangeRateProvider
{
	private readonly IFrankfurterClient  _frankfurterClient;
	private readonly IMemoryCache _cache;
	private readonly AsyncPolicyWrap _resiliencePolicy;

	public FrankfurterProvider(IFrankfurterClient frankfurterClient, IMemoryCache cache)
	{
		_frankfurterClient = frankfurterClient;
		_cache = cache;

		AsyncRetryPolicy retryPolicy = Policy
			.Handle<HttpRequestException>()
			.WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

		AsyncCircuitBreakerPolicy circuitBreakerPolicy = Policy
			.Handle<HttpRequestException>()
			.CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

		_resiliencePolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
	}

	public Task<Dictionary<string, string>?> GetSupportedCurrenciesAsync()
	{
		const string cacheKey = "SupportedCurrencyCodes";
		return _cache.GetOrCreateAsync(cacheKey, entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
			return _resiliencePolicy.ExecuteAsync(() =>
				_frankfurterClient.GetSupportedCurrenciesAsync());
		});
	}

	public Task<LatestRatesResponse?> GetLatestRatesAsync(
		string currencyCode,
		IEnumerable<string>? symbols = null)
	{
		var symbolsPart = symbols != null && symbols.Any()
			? string.Join(',', symbols)
			: "ALL";

		var cacheKey = $"LatestRates:{currencyCode}:{symbolsPart}";
		return _cache.GetOrCreateAsync(cacheKey, entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
			return _resiliencePolicy.ExecuteAsync(() =>
				_frankfurterClient.GetLatestRatesAsync(currencyCode, symbols));
		});
	}

	public Task<TimeSeriesResponse?> GetTimeSeriesAsync(
		DateTime startDate,
		DateTime endDate,
		string currencyCode,
		IEnumerable<string>? symbols = null)
	{
		var symbolsPart = symbols != null && symbols.Any()
			? string.Join(',', symbols)
			: "ALL";

		var cacheKey = $"TimeSeries:{currencyCode}:{startDate:yyyyMMdd}-{endDate:yyyyMMdd}:{symbolsPart}";
		return _cache.GetOrCreateAsync(cacheKey, entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
			return _resiliencePolicy.ExecuteAsync(() =>
				_frankfurterClient.GetTimeSeriesAsync(startDate, endDate, currencyCode, symbols));
		});
	}
}
