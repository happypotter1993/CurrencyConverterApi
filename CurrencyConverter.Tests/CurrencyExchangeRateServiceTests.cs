// CurrencyConverter.Tests/AllTests.cs
using CurrencyConverter.Controllers.Models;
using CurrencyConverter.Data.CurrencyExchangeRateProviders;
using CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;
using CurrencyConverter.Services;

using Microsoft.Extensions.Caching.Memory;

using Moq;

namespace CurrencyConverter.Tests
{
	#region CurrencyExchangeRateService Tests

	public class CurrencyExchangeRateServiceTests
	{
		private readonly Mock<ICurrencyExchangeRateProvider> _providerMock;
		private readonly CurrencyExchangeRateService _service;

		public CurrencyExchangeRateServiceTests()
		{
			_providerMock = new Mock<ICurrencyExchangeRateProvider>();
			_service = new CurrencyExchangeRateService(_providerMock.Object);
		}

		[Fact]
		public async Task GetSupportedCurrenciesAsync_ReturnsDtos()
		{
			// Arrange
			var data = new Dictionary<string, string>
			{
				["USD"] = "US Dollar",
				["EUR"] = "Euro"
			};
			_providerMock
				.Setup(p => p.GetSupportedCurrenciesAsync())
				.ReturnsAsync(data);

			// Act
			var result = (await _service.GetSupportedCurrenciesAsync()).ToList();

			// Assert
			Assert.Equal(2, result.Count);
			Assert.Contains(result, dto => dto.CurrencyCode == "USD" && dto.Currency == "US Dollar");
			Assert.Contains(result, dto => dto.CurrencyCode == "EUR" && dto.Currency == "Euro");
		}

		[Fact]
		public async Task ConvertAsync_ComputesCorrectly()
		{
			// Arrange
			var rates = new Dictionary<string, decimal> { ["EUR"] = 0.5m };
			var latest = new LatestRatesResponse("USD", "2025-05-13", rates);

			_providerMock
				.Setup(p => p.GetLatestRatesAsync("USD", It.Is<IEnumerable<string>>(s => s.Single() == "EUR")))
				.ReturnsAsync(latest);

			// Act
			ConversionResultDto dto = await _service.ConvertAsync("USD", "EUR", 100m);

			// Assert
			Assert.Equal("USD", dto.FromCurrencyCode);
			Assert.Equal("EUR", dto.ToCurrencyCode);
			Assert.Equal(100m, dto.OriginalAmount);
			Assert.Equal(50m, dto.ConvertedAmount);
			Assert.Equal(0.5m, dto.Rate);
			Assert.Equal(DateTime.Parse("2025-05-13"), dto.Timestamp);
		}

		[Fact]
		public async Task GetLatestRatesAsync_ForwardsProviderResult()
		{
			// Arrange
			var sample = new LatestRatesResponse("EUR", "2025-05-13",
				new Dictionary<string, decimal> { ["USD"] = 1.1m });
			_providerMock
				.Setup(p => p.GetLatestRatesAsync("EUR", null))
				.ReturnsAsync(sample);

			// Act
			LatestRatesResponse? result = await _service.GetLatestRatesAsync("EUR");

			// Assert
			if (result == null)
				Assert.Fail("Unable to get latest rates");
			Assert.Equal("EUR", result.Base);
			Assert.Equal("2025-05-13", result.Date);
			Assert.Equal(1.1m, result.Rates["USD"]);
		}

		[Fact]
		public async Task GetHistoricalRatesAsync_PaginatesCorrectly()
		{
			// Arrange
			var rates = new Dictionary<string, Dictionary<string, decimal>>
			{
				["2025-01-01"] = new Dictionary<string, decimal> { ["USD"] = 1m },
				["2025-01-02"] = new Dictionary<string, decimal> { ["USD"] = 1.1m },
				["2025-01-03"] = new Dictionary<string, decimal> { ["USD"] = 1.2m }
			};
			var tsResponse = new TimeSeriesResponse("USD", "2025-01-01", "2025-01-03", rates);

			// Arrange with explicit symbols array
			_providerMock
				.Setup(p => p.GetTimeSeriesAsync(
					new DateTime(2025, 1, 1),
					new DateTime(2025, 1, 3),
					"USD", null))
				.ReturnsAsync(tsResponse);

			// Act
			HistoricalRatesPagedDto page = await _service.GetHistoricalRatesAsync(
				new DateTime(2025, 1, 1),
				new DateTime(2025, 1, 3),
				"USD",
				pageNumber: 2,
				pageSize: 1);

			// Assert
			Assert.Equal(2, page.PageNumber);
			Assert.Equal(1, page.PageSize);
			Assert.Equal(3, page.TotalPages);
			Assert.Single(page.Rates);
			Assert.True(page.Rates.ContainsKey(new DateTime(2025, 1, 2)));
		}
	}

	#endregion

	#region FrankfurterProvider Tests

	public class FrankfurterProviderTests
	{
		private readonly Mock<IFrankfurterClient> _clientMock;
		private readonly IMemoryCache _cache;
		private readonly FrankfurterProvider _provider;

		public FrankfurterProviderTests()
		{
			_clientMock = new Mock<IFrankfurterClient>();
			_cache = new MemoryCache(new MemoryCacheOptions());
			_provider = new FrankfurterProvider(_clientMock.Object, _cache);
		}

		[Fact]
		public async Task GetLatestRatesAsync_UsesProviderAndPolicy()
		{
			// Arrange
			var latest = new LatestRatesResponse("EUR", "2025-05-13",
				new Dictionary<string, decimal> { ["USD"] = 1.1m });
			_clientMock
				.Setup(c => c.GetLatestRatesAsync("EUR", null))
				.ReturnsAsync(latest);

			// Act
			LatestRatesResponse? result = await _provider.GetLatestRatesAsync("EUR");

			// Assert
			Assert.Equal(latest, result);
		}

		[Fact]
		public async Task GetTimeSeriesAsync_UsesProviderAndPolicy()
		{
			// Arrange
			var ts = new TimeSeriesResponse("EUR", "2025-01-01", "2025-01-03",
				new Dictionary<string, Dictionary<string, decimal>>
				{
					["2025-01-01"] = new Dictionary<string, decimal> { ["USD"] = 1m }
				});
			_clientMock
				.Setup(c => c.GetTimeSeriesAsync(
					new DateTime(2025, 1, 1),
					new DateTime(2025, 1, 3),
					"EUR", null))
				.ReturnsAsync(ts);

			// Act
			TimeSeriesResponse? result = await _provider.GetTimeSeriesAsync(
				new DateTime(2025, 1, 1),
				new DateTime(2025, 1, 3),
				"EUR");

			// Assert
			Assert.Equal(ts, result);
		}
	}

	#endregion
}
