using System.Diagnostics;
namespace CurrencyConverter.Middleware
{
	public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
	{
		private readonly RequestDelegate _next = next;
		private readonly ILogger<RequestLoggingMiddleware> _logger = logger;

		public async Task Invoke(HttpContext context)
		{
			var stopwatch = Stopwatch.StartNew();

			// Extract basic info
			var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
			var method = context.Request.Method;
			PathString path = context.Request.Path;

			// Try to get ClientId from token
			var clientId = context.User?.Claims?.FirstOrDefault(c => c.Type == "ClientId")?.Value ?? "anonymous";

			await _next(context);

			stopwatch.Stop();

			var statusCode = context.Response.StatusCode;
			var elapsedMs = stopwatch.ElapsedMilliseconds;

			_logger.LogInformation("Request {Method} {Path} from IP {IP} [ClientId: {ClientId}] responded {StatusCode} in {Elapsed} ms",
				method, path, ip, clientId, statusCode, elapsedMs);
		}
	}

}
