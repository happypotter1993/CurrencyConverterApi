using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using CurrencyConverter.Controllers.Models;
using CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;
using CurrencyConverter.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.Controllers
{
	[ApiController]
	[Route("api/v1")]
	[Authorize]
	public class CurrencyExchangeRateController : ControllerBase
	{
		private static readonly string[] UnsupportedCurrencies = ["TRY", "PLN", "THB", "MXN"];
		private readonly ICurrencyExchangeRateService _service;

		public CurrencyExchangeRateController(ICurrencyExchangeRateService service)
			=> _service = service;

		// GET /api/v1/currencies
		[HttpGet("currencies")]
		public async Task<ActionResult<IEnumerable<SupportedCurrencyDto>>> GetCurrenciesAsync()
		{
			var currencies = await _service.GetSupportedCurrenciesAsync();
			return Ok(currencies);
		}

		// GET /api/v1/conversions?fromCurrencyCode=USD&toCurrencyCode=EUR&amount=100
		[HttpGet("conversions")]
		public async Task<ActionResult<ConversionResultDto>> GetConversionAsync(
			[FromQuery, Required, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
			string fromCurrencyCode,
			[FromQuery, Required, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
			string toCurrencyCode,
			[FromQuery, Required, Range(0.01, double.MaxValue)]
			decimal amount
		)
		{
			if (UnsupportedCurrencies.Contains(fromCurrencyCode) ||
				UnsupportedCurrencies.Contains(toCurrencyCode))
			{
				return BadRequest($"Conversion involving {string.Join(", ", UnsupportedCurrencies)} is not supported.");
			}

			var result = await _service.ConvertAsync(fromCurrencyCode, toCurrencyCode, amount);
			return Ok(result);
		}

		// GET /api/v1/exchange-rates/latest?currencyCode=EUR
		[HttpGet("exchange-rates/latest")]
		public async Task<ActionResult<LatestRatesResponse>> GetLatestRatesAsync(
			[FromQuery, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
			string currencyCode = "EUR"
		)
		{
			var result = await _service.GetLatestRatesAsync(currencyCode);
			if (result == null)
				return NotFound();
			return Ok(result);
		}

		// GET /api/v1/exchange-rates/historical?currencyCode=EUR&startDate=2025-04-01&endDate=2025-04-30&pageNumber=1&pageSize=10
		[HttpGet("exchange-rates/historical")]
		public async Task<ActionResult<HistoricalRatesPagedDto>> GetHistoricalRatesAsync(
			[FromQuery, StringLength(3, MinimumLength = 3), RegularExpression(@"^[A-Z]{3}$")]
			string currencyCode = "EUR",
			[FromQuery] DateTime? startDate = null,
			[FromQuery] DateTime? endDate = null,
			[FromQuery, Range(1, int.MaxValue)]
			int pageNumber = 1,
			[FromQuery, Range(1, 100)]
			int pageSize = 10
		)
		{
			var from = startDate ?? DateTime.UtcNow.AddMonths(-1).Date;
			var to   = endDate   ?? DateTime.UtcNow.Date;

			var paged = await _service.GetHistoricalRatesAsync(
				from, to, currencyCode, pageNumber, pageSize
			);

			return Ok(paged);
		}
	}
}
