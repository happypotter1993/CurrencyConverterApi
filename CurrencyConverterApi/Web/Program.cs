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
using Microsoft.AspNetCore.Builder;
using Frankfurter.API.Client.DependencyInjection;
using CurrencyConverter.Data;
using CurrencyConverter.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────
// Structured Logging: Serilog (file + console)
// ─────────────────────────────────────────────
builder.Host.UseSerilog((context, config) =>
{
	config.ReadFrom.Configuration(context.Configuration)
		  .Enrich.FromLogContext()
		  .WriteTo.Console()
		  .WriteTo.File("Logs/api-log.txt", rollingInterval: RollingInterval.Day);
});

// ─────────────────────────────────────────────
// Services: Controllers, Caching, HTTP Context
// ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// ─────────────────────────────────────────────
// Rate Limiting (Fixed Window)
// ─────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
	options.AddFixedWindowLimiter("default", options =>
	{
		options.PermitLimit = 20;
		options.Window = TimeSpan.FromSeconds(10);
		options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
		options.QueueLimit = 5;
	});
});

// ─────────────────────────────────────────────
// OpenTelemetry Tracing: HTTP + ASP.NET Core
// ─────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
	.WithTracing(tracing =>
	{
		tracing.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CurrencyConverter"))
			   .AddAspNetCoreInstrumentation()
			   .AddHttpClientInstrumentation();
	});

// ─────────────────────────────────────────────
// Authentication: JWT Bearer (Dev Token)
// ─────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = false,
			ValidateAudience = false,
			ValidateLifetime = false,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(
				Encoding.UTF8.GetBytes("dev-assignment-key-ONLY_NOT_FOR_PROD"))
		};
	});

builder.Services.AddAuthorization();

// ─────────────────────────────────────────────
// Swagger + JWT Token UI Support
// ─────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "Currency Converter API",
		Version = "v1"
	});

	options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Description = "Enter a valid JWT token like: Bearer {token}",
		Name = "Authorization",
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Scheme = "bearer",
		BearerFormat = "JWT"
	});

	options.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			new List<string>()
		}
	});
});

// ─────────────────────────────────────────────
// HttpClient + Polly (Retry + Circuit Breaker)
// ─────────────────────────────────────────────
builder.Services.AddHttpClient<ICurrencyProvider, FrankfurterProvider>()
	.AddPolicyHandler(GetRetryPolicy())
	.AddPolicyHandler(GetCircuitBreakerPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
	HttpPolicyExtensions
		.HandleTransientHttpError()
		.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
	HttpPolicyExtensions
		.HandleTransientHttpError()
		.CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));

// ─────────────────────────────────────────────
// Dependency Injection (Core Services)
// ─────────────────────────────────────────────
//builder.Services.AddScoped<ICurrencyService, CurrencyService>();
builder.Services.AddFrankfurterApiClient();

// ─────────────────────────────────────────────
// Build & Configure Middleware Pipeline
// ─────────────────────────────────────────────
WebApplication app = builder.Build();

// Log + Trace incoming requests (task PDF compliant)
app.UseRateLimiter();
app.UseMiddleware<RequestLoggingMiddleware>(); // Logs IP, ClientId, Path, Status, Timing

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
