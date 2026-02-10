using ForaFinServices.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace ForaFin.Utils;
public class JwtAuthenticationMiddleware : IFunctionsWorkerMiddleware
{
    public JwtAuthenticationMiddleware()
    {
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;
        var publicFunctions = new List<string> { "GetToken", "HealthCheck" };
        if (publicFunctions.Contains(functionName))
        {
            await next(context);
            return;
        }

        var tokenValidator = context.InstanceServices.GetRequiredService<ITokenValidator>();

        // 1. Extraer el HttpRequestData del contexto
        var req = await context.GetHttpRequestDataAsync();
        if (req == null) { await next(context); return; }

        // Extraer Header
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
        {
            await Unauthorized(context, req, "Authorization Header missing.");
            return;
        }

        var bearerToken = authHeaders.FirstOrDefault(h => h.StartsWith("Bearer "));
        if (string.IsNullOrEmpty(bearerToken))
        {
            await Unauthorized(context, req, "Bearer token missing.");
            return;
        }

        var token = bearerToken.Replace("Bearer ", "");
        var user = await tokenValidator.ValidateTokenAsync(token);
        if (user == null || user.Claims == null || !user.Claims.Any(c => c.Type == "name"))
        {
            await Unauthorized(context, req, "Invalid token.");
            return;
        }

        // Si llega aquí, el token es válido. Puedes obtener el nombre del usuario:
        var userName = user.Claims.First(c => c.Type == "name").Value;
        context.Items["User"] = user;
        // return userName;
        await next(context);
    }

    private async Task Unauthorized(FunctionContext context, HttpRequestData req, string message = "Unauthorized")
    {
        var response = req.CreateResponse(System.Net.HttpStatusCode.Unauthorized);
        await response.WriteAsJsonAsync(new { Message = message });
        context.GetInvocationResult().Value = response;
    }
}