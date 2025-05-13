# Currency Converter API

A fast, resilient ASP NET Core service for currency lookups and conversions, backed by the Frankfurter API.

## Features

- **Supported Currencies**: list codes & names  
- **Conversions**: amount → target currency  
- **Latest Rates**: fetch today’s rates (default base EUR)  
- **Historical Rates**: paginated time series  
- **Resilience**: in-memory caching, Polly retry & circuit breaker  
- **Auth & Security**: JWT Bearer, rate-limiting  
- **Observability**: Serilog logging, OpenTelemetry tracing  
- **Containerized**: Dockerfile included  

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)  
- [Docker](https://www.docker.com/get-started) (optional)  

## Getting Started

1. Clone the repo  
2. Run the API:  
   ```bash
   dotnet run --project CurrencyConverter
   ```  
3. Browse to Swagger UI to explore endpoints:  

_Alternative via Docker_:  
```bash
docker build -t currency-converter .
docker run -d -p 5000:80 currency-converter
```

## Configuration

Copy `appsettings.json` and set your JWT secret (min 32 bytes):

```jsonc
{
  "Jwt": {
    "Key": "your-32+-char-secret"
  }
}
```

## API Endpoints

| Method | Path                                    | Description                                    |
| ------ | --------------------------------------- | ---------------------------------------------- |
| GET    | `/api/v1/currencies`                    | all supported currencies                       |
| GET    | `/api/v1/conversions`                   | convert?fromCurrencyCode=USD&toCurrencyCode=EUR&amount=100 |
| GET    | `/api/v1/exchange-rates/latest`         | latest?currencyCode=EUR                        |
| GET    | `/api/v1/exchange-rates/historical`     | historical?currencyCode=EUR&startDate=YYYY-MM-DD&endDate=YYYY-MM-DD&pageNumber=1&pageSize=10 |

All `/api/v1` routes require `Authorization: Bearer <token>`.

### Generate & Validate JWT

```bash
# generate
curl -X POST http://localhost:5000/api/v1/auth/generate

# validate
curl -H "Authorization: Bearer <token>" http://localhost:5000/api/v1/auth/validate
```

## Test Coverage

A test-coverage report is attached in the `/coverage-report` folder. Open `coverage-report/index.html` to view.

## Assumptions

- All date/time values are assumed to be in UTC.  
- Currency amounts use `decimal` type with up to 6 decimal places precision.  
- The API excludes TRY, PLN, THB, and MXN as unsupported currencies.  
- Default base currency for rates is EUR if none is supplied.  

## Future Enhancements

- **Add Authentication for Users**: Currently the API to generate JWT Tokens does not require authentication, depends on the business use-case
- **Multiple Provider Support**: add more exchange-rate providers.  
- **API Level Caching**: Use an API based caching mechanism like in AWS API Gateway
- **Test Coverage**: Improve test coverage, currently it only covers the core logic

---