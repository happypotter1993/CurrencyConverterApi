using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using Polly;
using Polly.Extensions.Http;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using CurrencyConverter.Middleware;
using CurrencyConverter.Services;
using CurrencyConverter.Data.CurrencyExchangeRateProviders;
using CurrencyConverter.Data.CurrencyExchangeRateProviders.Frankfurter;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
	cfg.ReadFrom.Configuration(ctx.Configuration)
	   .Enrich.FromLogContext()
	   .WriteTo.Console()
	   .WriteTo.File("Logs/api-log.txt", rollingInterval: RollingInterval.Day)
);

// ─── Framework services ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// ─── Rate Limiting ──────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
	opts.AddFixedWindowLimiter("default", o =>
	{
		o.PermitLimit = 20;
		o.Window = TimeSpan.FromSeconds(10);
		o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
		o.QueueLimit = 5;
	});
});

// ─── OpenTelemetry ──────────────────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
	.WithTracing(t => t
		.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CurrencyConverter"))
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
	);

// ─── Authentication + Authorization ──────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		var signingKey = builder.Configuration["Jwt:Key"];
		if (string.IsNullOrWhiteSpace(signingKey))
			throw new InvalidOperationException("Please set Jwt:Key in appsettings");

		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateLifetime = false,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes(signingKey))
		};
	});

builder.Services.AddAuthorization();


// ─── Swagger ────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
	o.SwaggerDoc("v1", new OpenApiInfo { Title = "Currency Converter API", Version = "v1" });
	o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "Enter a valid JWT like: Bearer {token}",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT"
	});
	o.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		[new OpenApiSecurityScheme
		{
			Reference = new OpenApiReference
			{
				Type = ReferenceType.SecurityScheme,
				Id = "Bearer"
			}
		}
		] = Array.Empty<string>()
	});
});

// ─── HttpClient + Polly for FrankfurterClient ────────────────────────────────
builder.Services
	.AddHttpClient<IFrankfurterClient, FrankfurterClient>(client =>
	{
		client.BaseAddress = new Uri("https://api.frankfurter.dev/");
	})
	.AddPolicyHandler(GetRetryPolicy())
	.AddPolicyHandler(GetCircuitBreakerPolicy());

// ─── Your services ──────────────────────────────────────────────────────────
builder.Services.AddScoped<ICurrencyExchangeRateProvider, FrankfurterProvider>();
builder.Services.AddScoped<ICurrencyExchangeRateService, CurrencyExchangeRateService>();

var app = builder.Build();

// ─── Pipeline ───────────────────────────────────────────────────────────────
app.UseRateLimiter();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();


// ─── Polly helpers ──────────────────────────────────────────────────────────
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
	HttpPolicyExtensions
		.HandleTransientHttpError()
		.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
	HttpPolicyExtensions
		.HandleTransientHttpError()
		.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
