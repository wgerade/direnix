using System.Security.Cryptography;
using Direnix.Core.Auth;
using Direnix.Core.Storage;
using Direnix.Service.Configuration;
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
        endpoints.MapGet("/api/v1/auth/me", async (IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct) =>
        {
            // Portátil: sessão única implícita, sem setup/login. Trata como administrador.
            if (portable.IsPortable)
            {
                return Results.Ok(new { needsSetup = false, authenticated = true, username = portable.Operator, portable = true, isAdmin = true, role = nameof(Direnix.Core.Identity.AppRole.LocalAdmin) });
            }

            var needsSetup = await store.GetUserCountAsync(ct) == 0;
            var session = await ResolveSessionAsync(store, http, ct);
            // AppSession.UserId guarda o *username* (ver IssueSessionAsync).
            var user = session is not null ? await store.GetUserByNameAsync(session.UserId, ct) : null;
            return Results.Ok(new
            {
                needsSetup,
                authenticated = session is not null,
                username = user?.Username ?? session?.UserId,
                portable = false,
                isAdmin = user?.Role == nameof(Direnix.Core.Identity.AppRole.LocalAdmin),
                role = user?.Role,
                lastLogin = user?.LastLogin
            });
        });

        endpoints.MapPost("/api/v1/auth/setup", async (CredentialBody body, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            if (await store.GetUserCountAsync(ct) > 0)
            {
                return Results.Conflict(new { error = "Administrador ja configurado." });
            }
            if (string.IsNullOrWhiteSpace(body.Username))
            {
                return Results.BadRequest(new { error = "Informe um nome de usuario." });
            }
            if (PasswordPolicy.Validate(body.Password) is { } policyError)
            {
                return Results.BadRequest(new { error = policyError });
            }

            var hash = PasswordHasher.Hash(body.Password!);
            var user = new AppUserRecord(Guid.NewGuid().ToString("N"), body.Username.Trim(),
                hash.Hash, hash.Salt, hash.Iterations, "LocalAdmin", DateTimeOffset.UtcNow);
            await store.CreateUserAsync(user, ct);
            await IssueSessionAsync(store, http, user, ct);
            await store.RegisterSuccessfulLoginAsync(user.Username, DateTimeOffset.UtcNow, ct);
            await PortalAudit.LogAsync(store, http, "AdminCreated", "User", user.Username, "Success");
            return Results.Ok(new { authenticated = true, username = user.Username });
        });

        endpoints.MapPost("/api/v1/auth/login", async (CredentialBody body, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            var now = DateTimeOffset.UtcNow;
            var user = string.IsNullOrWhiteSpace(body.Username) ? null : await store.GetUserByNameAsync(body.Username.Trim(), ct);

            // Bloqueio por forca bruta: conta trancada nao aceita login ate expirar.
            if (user is not null && user.IsLockedNow(now))
            {
                var mins = (int)Math.Ceiling((user.LockedUntil!.Value - now).TotalMinutes);
                await PortalAudit.LogAsync(store, http, "LoginBlocked", "User", user.Username, "Failure");
                return Results.Json(new { error = $"Conta bloqueada por tentativas. Tente novamente em {mins} min." }, statusCode: 423);
            }

            if (user is null || string.IsNullOrEmpty(body.Password) ||
                !PasswordHasher.Verify(body.Password, user.PasswordHash, user.Salt, user.Iterations))
            {
                if (user is not null)
                {
                    await store.RegisterFailedLoginAsync(user.Username, now, ct);
                }
                await PortalAudit.LogAsync(store, http, "LoginFailed", "User", body.Username ?? "(vazio)", "Failure");
                return Results.Json(new { error = "Usuario ou senha invalidos." }, statusCode: 401);
            }

            await IssueSessionAsync(store, http, user, ct);
            await store.RegisterSuccessfulLoginAsync(user.Username, now, ct);
            await PortalAudit.LogAsync(store, http, "LoginSuccess", "User", user.Username, "Success");
            return Results.Ok(new { authenticated = true, username = user.Username });
        });

        // Troca da própria senha (usuário logado). Exige a senha atual.
        endpoints.MapPost("/api/v1/auth/change-password", async (ChangePasswordBody body, IProductStore store, HttpContext http, CancellationToken ct) =>
        {
            var session = await ResolveSessionAsync(store, http, ct);
            if (session is null)
            {
                return Results.Json(new { error = "Autenticacao necessaria." }, statusCode: 401);
            }
            var user = await store.GetUserByNameAsync(session.UserId, ct);
            if (user is null || string.IsNullOrEmpty(body.CurrentPassword) ||
                !PasswordHasher.Verify(body.CurrentPassword, user.PasswordHash, user.Salt, user.Iterations))
            {
                return Results.Json(new { error = "Senha atual incorreta." }, statusCode: 400);
            }
            if (PasswordPolicy.Validate(body.NewPassword) is { } policyError)
            {
                return Results.BadRequest(new { error = policyError });
            }
            var hash = PasswordHasher.Hash(body.NewPassword!);
            await store.UpdatePasswordAsync(user.UserId, hash.Hash, hash.Salt, hash.Iterations, ct);
            // UpdatePassword encerra as sessões do usuário; reemite a atual para não deslogar.
            await IssueSessionAsync(store, http, user, ct);
            await PortalAudit.LogAsync(store, http, "PasswordChanged", "User", user.Username, "Success");
            return Results.Ok(new { changed = true });
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
public sealed record ChangePasswordBody(string? CurrentPassword, string? NewPassword);
