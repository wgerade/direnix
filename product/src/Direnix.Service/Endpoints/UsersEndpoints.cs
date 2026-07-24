using Direnix.Core.Auth;
using Direnix.Core.Identity;
using Direnix.Core.Storage;
using Direnix.Service.Auth;
using Direnix.Service.Configuration;

namespace Direnix.Service.Endpoints;

/// <summary>
/// Gestão de usuários locais do portal (apenas LocalAdmin). Modelo de 2 papéis
/// exposto na UI: administrador (LocalAdmin) e somente leitura (ReadOnlyTechnical).
/// </summary>
public static class UsersEndpoints
{
    private static readonly string Admin = nameof(AppRole.LocalAdmin);
    private static readonly string ReadOnly = nameof(AppRole.ReadOnlyTechnical);

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/users");

        group.MapGet("/", async (IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct) =>
        {
            var (isAdmin, selfId) = await ResolveAsync(store, http, portable, ct);
            if (!isAdmin)
            {
                return Results.Json(new { error = "Apenas administradores podem gerenciar usuarios." }, statusCode: 403);
            }
            var now = DateTimeOffset.UtcNow;
            var users = await store.ListUsersAsync(ct);
            var online = (await store.ListActiveSessionUsernamesAsync(now, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return Results.Ok(new
            {
                portable = portable.IsPortable,
                items = users.Select(u => new
                {
                    userId = u.UserId,
                    username = u.Username,
                    role = u.Role,
                    createdAt = u.CreatedAt,
                    isSelf = u.UserId == selfId,
                    lastLogin = u.LastLogin,
                    online = online.Contains(u.Username),
                    locked = u.IsLockedNow(now)
                })
            });
        });

        group.MapPost("/", async (UserCreateBody body, IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct) =>
        {
            var (isAdmin, _) = await ResolveAsync(store, http, portable, ct);
            if (!isAdmin)
            {
                return Results.Json(new { error = "Apenas administradores podem gerenciar usuarios." }, statusCode: 403);
            }
            var role = NormalizeRole(body.Role);
            if (role is null)
            {
                return Results.BadRequest(new { error = "Papel invalido." });
            }
            if (string.IsNullOrWhiteSpace(body.Username))
            {
                return Results.BadRequest(new { error = "Informe um nome de usuario." });
            }
            if (PasswordPolicy.Validate(body.Password) is { } policyError)
            {
                return Results.BadRequest(new { error = policyError });
            }
            if (await store.GetUserByNameAsync(body.Username.Trim(), ct) is not null)
            {
                return Results.Conflict(new { error = "Ja existe um usuario com esse nome." });
            }

            var hash = PasswordHasher.Hash(body.Password!);
            var user = new AppUserRecord(Guid.NewGuid().ToString("N"), body.Username.Trim(),
                hash.Hash, hash.Salt, hash.Iterations, role, DateTimeOffset.UtcNow);
            await store.CreateUserAsync(user, ct);
            await PortalAudit.LogAsync(store, http, "UserCreated", "User", $"{user.Username} ({role})", "Success");
            return Results.Ok(new { userId = user.UserId, username = user.Username, role });
        });

        group.MapPut("/{userId}/role", async (string userId, UserRoleBody body, IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct) =>
        {
            var (isAdmin, _) = await ResolveAsync(store, http, portable, ct);
            if (!isAdmin)
            {
                return Results.Json(new { error = "Apenas administradores podem gerenciar usuarios." }, statusCode: 403);
            }
            var role = NormalizeRole(body.Role);
            if (role is null)
            {
                return Results.BadRequest(new { error = "Papel invalido." });
            }
            var target = await store.GetUserByIdAsync(userId, ct);
            if (target is null)
            {
                return Results.NotFound(new { error = "Usuario nao encontrado." });
            }
            // Não deixar a conta ficar sem nenhum administrador.
            if (target.Role == Admin && role != Admin && await store.CountUsersByRoleAsync(Admin, ct) <= 1)
            {
                return Results.BadRequest(new { error = "Deve existir ao menos um administrador." });
            }
            await store.UpdateUserRoleAsync(userId, role, ct);
            await PortalAudit.LogAsync(store, http, "UserRoleChanged", "User", $"{target.Username} -> {role}", "Success");
            return Results.Ok(new { userId, role });
        });

        group.MapPut("/{userId}/password", async (string userId, UserPasswordBody body, IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct) =>
        {
            var (isAdmin, _) = await ResolveAsync(store, http, portable, ct);
            if (!isAdmin)
            {
                return Results.Json(new { error = "Apenas administradores podem gerenciar usuarios." }, statusCode: 403);
            }
            if (PasswordPolicy.Validate(body.Password) is { } policyError)
            {
                return Results.BadRequest(new { error = policyError });
            }
            var target = await store.GetUserByIdAsync(userId, ct);
            if (target is null)
            {
                return Results.NotFound(new { error = "Usuario nao encontrado." });
            }
            var hash = PasswordHasher.Hash(body.Password!);
            // Reset zera lockout e encerra as sessões do alvo (força novo login).
            await store.UpdatePasswordAsync(userId, hash.Hash, hash.Salt, hash.Iterations, ct);
            await PortalAudit.LogAsync(store, http, "PasswordReset", "User", target.Username, "Success");
            return Results.Ok(new { userId, reset = true });
        });

        group.MapDelete("/{userId}", async (string userId, IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct) =>
        {
            var (isAdmin, selfId) = await ResolveAsync(store, http, portable, ct);
            if (!isAdmin)
            {
                return Results.Json(new { error = "Apenas administradores podem gerenciar usuarios." }, statusCode: 403);
            }
            if (userId == selfId)
            {
                return Results.BadRequest(new { error = "Voce nao pode excluir a propria conta." });
            }
            var target = await store.GetUserByIdAsync(userId, ct);
            if (target is null)
            {
                return Results.NotFound(new { error = "Usuario nao encontrado." });
            }
            if (target.Role == Admin && await store.CountUsersByRoleAsync(Admin, ct) <= 1)
            {
                return Results.BadRequest(new { error = "Deve existir ao menos um administrador." });
            }
            await store.DeleteUserAsync(userId, ct);
            await PortalAudit.LogAsync(store, http, "UserDeleted", "User", target.Username, "Success");
            return Results.Ok(new { deleted = userId });
        });

        return endpoints;
    }

    // LocalAdmin? + id do usuário da sessão. Portátil = admin (sessão local única, sem login).
    private static async Task<(bool isAdmin, string? selfId)> ResolveAsync(IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct)
    {
        if (portable.IsPortable)
        {
            return (true, null);
        }
        var session = await AuthEndpoints.ResolveSessionAsync(store, http, ct);
        if (session is null)
        {
            return (false, null);
        }
        // AppSession.UserId guarda o *username* (ver IssueSessionAsync); resolve o
        // registro para obter papel e o UserId real (usado em isSelf / excluir-self).
        var user = await store.GetUserByNameAsync(session.UserId, ct);
        return (user is not null && user.Role == Admin, user?.UserId);
    }

    private static string? NormalizeRole(string? role) => role switch
    {
        _ when string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase) => Admin,
        _ when string.Equals(role, ReadOnly, StringComparison.OrdinalIgnoreCase) => ReadOnly,
        _ => null
    };
}

public sealed record UserCreateBody(string? Username, string? Password, string? Role);
public sealed record UserRoleBody(string? Role);
public sealed record UserPasswordBody(string? Password);
