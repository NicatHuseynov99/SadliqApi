using Business.Abstract;
using Entities.Concrete;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;

namespace SadliqApi.Audit
{
    // Hər sorğunu tutub jurnala yazır. Servis kodu IAuditContext ilə izah əlavə edərsə
    // həmin mətn işlədilir, əks halda lüğətdən avtomatik mətn qurulur.
    public class AuditActionFilter : IAsyncActionFilter
    {
        // Jurnalda heç vaxt görünməməli sahələr
        private static readonly string[] SensitiveKeys =
        {
            "password", "currentpassword", "newpassword", "repeatpassword", "token", "passwordhash", "passwordsalt"
        };

        // Oxuma sorğularından yalnız bunlar yazılır — qalanları jurnalı doldurardı
        private static readonly string[] LoggedReadControllers = { "Admin", "Audit" };

        private readonly IAuditWriter _writer;
        private readonly IAuditContext _auditContext;

        public AuditActionFilter(IAuditWriter writer, IAuditContext auditContext)
        {
            _writer = writer;
            _auditContext = auditContext;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var method = http.Request.Method;
            var controller = context.RouteData.Values["controller"]?.ToString() ?? string.Empty;
            var action = context.RouteData.Values["action"]?.ToString() ?? string.Empty;

            var shouldLog = method is "POST" or "PUT" or "DELETE"
                            || LoggedReadControllers.Contains(controller);

            // Sorğunun gövdəsini həssas sahələr gizlədilmiş halda saxlayırıq
            var payload = shouldLog ? BuildPayload(context.ActionArguments) : null;

            var stopwatch = Stopwatch.StartNew();
            var executed = await next();
            stopwatch.Stop();

            if (!shouldLog)
                return;

            var statusCode = executed.HttpContext.Response.StatusCode;
            var isSuccess = executed.Exception is null && statusCode < 400;

            var (defaultAction, defaultText) = AuditDictionary.Resolve(controller, action);

            // Giriş/qeydiyyatda istifadəçi hələ token daşımır — servis özü bildirir
            var userName = _auditContext.ActorName
                           ?? http.User.FindFirstValue(ClaimTypes.Name)
                           ?? "Qonaq";

            var userRole = _auditContext.ActorRole.HasValue
                ? (UserRole)_auditContext.ActorRole.Value
                : TryGetRole(http.User);

            var log = new AuditLog
            {
                UserId = _auditContext.ActorId ?? TryGetUserId(http.User),
                UserFullName = userName,
                UserRole = userRole,
                Action = _auditContext.Action ?? defaultAction,
                Description = _auditContext.Description ?? $"{userName} {defaultText}",
                EntityType = _auditContext.EntityType,
                EntityId = _auditContext.EntityId,
                VenueId = _auditContext.VenueId,
                HttpMethod = method,
                Path = http.Request.Path.Value ?? string.Empty,
                StatusCode = statusCode,
                DurationMs = stopwatch.ElapsedMilliseconds,
                IpAddress = http.Connection.RemoteIpAddress?.ToString(),
                UserAgent = http.Request.Headers.UserAgent.ToString(),
                IsSuccess = isSuccess,
                ErrorMessage = executed.Exception?.Message,
                Payload = payload,
                CreatedAt = DateTime.UtcNow
            };

            _writer.Enqueue(log);
        }

        private static int? TryGetUserId(ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        private static UserRole? TryGetRole(ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRole>(value, true, out var role) ? role : null;
        }

        // Həssas sahələri *** ilə əvəz edib qısa JSON qurur
        private static string? BuildPayload(IDictionary<string, object?> arguments)
        {
            if (arguments.Count == 0)
                return null;

            try
            {
                var safe = new Dictionary<string, object?>();

                foreach (var (key, value) in arguments)
                {
                    if (value is null)
                        continue;

                    safe[key] = Sanitize(value);
                }

                var json = JsonSerializer.Serialize(safe);
                return json.Length > 2000 ? json[..2000] : json;
            }
            catch
            {
                // Gövdə serializasiya oluna bilmirsə jurnal yenə də yazılmalıdır
                return null;
            }
        }

        private static object? Sanitize(object value)
        {
            if (value is string or int or long or bool or decimal or double or DateTime)
                return value;

            var result = new Dictionary<string, object?>();

            foreach (var property in value.GetType().GetProperties())
            {
                if (SensitiveKeys.Contains(property.Name.ToLowerInvariant()))
                {
                    result[property.Name] = "***";
                    continue;
                }

                try
                {
                    result[property.Name] = property.GetValue(value)?.ToString();
                }
                catch
                {
                    result[property.Name] = null;
                }
            }

            return result;
        }
    }
}
