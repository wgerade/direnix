using System.Security.Cryptography;
using Direnix.Core.Auth;
using Direnix.Core.Storage;
using Direnix.Service.Endpoints;

namespace Direnix.Service.Auth;

/// <summary>
/// Login local mínimo (Bloco B-min): bootstrap do admin no 1º uso, login/logout,
/// sessão por cookie HttpOnly. Não é RBAC completo — apenas impede reconfiguração
/// anônima do portal (agendamento, perfis, exceções, disparo de coleta).
/// </summary>
public static class AuthEndpoints
{
    public const string CookieName = "adc_session";
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(12);

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/auth/me", async (IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            var needsSetup = await store.GetUserCountAsync(ct) == 0;
            var session = await ResolveSessionAsync(store, http, ct);
            return Results.Ok(new
            {
                needsSetup,
                authenticated = session is not null,
                username = session?.UserId
            });
        });

        endpoints.MapPost("/api/v1/auth/setup", async (CredentialBody body, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            if (await store.GetUserCountAsync(ct) > 0)
            {
                return Results.Conflict(new { error = "Administrador ja configurado." });
            }
            if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrEmpty(body.Password) || body.Password.Length < 8)
            {
                return Results.BadRequest(new { error = "Informe usuario e uma senha de pelo menos 8 caracteres." });
            }

            var hash = PasswordHasher.Hash(body.Password);
            var user = new AppUserRecord(Guid.NewGuid().ToString("N"), body.Username.Trim(),
                hash.Hash, hash.Salt, hash.Iterations, "LocalAdmin", DateTimeOffset.UtcNow);
            await store.CreateUserAsync(user, ct);
            await IssueSessionAsync(store, http, user, ct);
            await PortalAudit.LogAsync(store, http, "AdminCreated", "User", user.Username, "Success");
            return Results.Ok(new { authenticated = true, username = user.Username });
        });

        endpoints.MapPost("/api/v1/auth/login", async (CredentialBody body, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            var user = string.IsNullOrWhiteSpace(body.Username) ? null : await store.GetUserByNameAsync(body.Username.Trim(), ct);
            if (user is null || string.IsNullOrEmpty(body.Password) ||
                !PasswordHasher.Verify(body.Password, user.PasswordHash, user.Salt, user.Iterations))
            {
                await PortalAudit.LogAsync(store, http, "LoginFailed", "User", body.Username ?? "(vazio)", "Failure");
                return Results.Json(new { error = "Usuario ou senha invalidos." }, statusCode: 401);
            }

            await IssueSessionAsync(store, http, user, ct);
            await PortalAudit.LogAsync(store, http, "LoginSuccess", "User", user.Username, "Success");
            return Results.Ok(new { authenticated = true, username = user.Username });
        });

        endpoints.MapPost("/api/v1/auth/logout", async (IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            var token = http.Request.Cookies[CookieName];
            if (!string.IsNullOrEmpty(token))
            {
                var existing = await store.GetSessionAsync(token, ct);
                await store.DeleteSessionAsync(token, ct);
                await PortalAudit.LogAsync(store, http, "Logout", "User", existing?.UserId ?? "—", "Success");
            }
            http.Response.Cookies.Delete(CookieName);
            return Results.Ok(new { authenticated = false });
        });

        return endpoints;
    }

    /// <summary>Valida o cookie de sessão; retorna a sessão ativa ou null.</summary>
    public static async Task<AppSession?> ResolveSessionAsync(IProductStore store, HttpContext http, CancellationToken ct)
    {
        var token = http.Request.Cookies[CookieName];
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }
        var session = await store.GetSessionAsync(token, ct);
        return session is not null && session.ExpiresAt > DateTimeOffset.UtcNow ? session : null;
    }

    private static async Task IssueSessionAsync(IProductStore store, HttpContext http, AppUserRecord user, CancellationToken ct)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var session = new AppSession(token, user.Username, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.Add(SessionLifetime));
        await store.CreateSessionAsync(session, ct);
        http.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Expires = session.ExpiresAt
        });
    }
}

public sealed record CredentialBody(string? Username, string? Password);
