using Frankfurter.API.Client.Domain;
using Frankfurter.API.Client;
using System.Linq;
using CurrencyConverter.Services.Models;

namespace CurrencyConverter.Data
{
	public class FrankfurterProvider(IFrankfurterClient client) : ICurrencyProvider
	{
		private readonly IFrankfurterClient _client = client;

		public async Task<Dictionary<CurrencyCode, decimal>> GetLatestRatesAsync(CurrencyCode baseCurrency)
		{

			throw new NotImplementedException();
		}

		public async Task<decimal> ConvertAsync(CurrencyCode from, CurrencyCode to, decimal amount)
		{
			throw new NotImplementedException();
		}

		public async Task<PaginatedHistoricalRates> GetHistoricalRatesAsync(
			CurrencyCode baseCurrency,
			List<CurrencyCode> toCurrencies,
			DateTime from,
			DateTime to,
			int page = 1,
			int pageSize = 100)
		{
			throw new NotImplementedException();
		}
	}
}