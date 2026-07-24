using Direnix.Core.Auth;
using Direnix.Core.Identity;
using Direnix.Core.Storage;
using Direnix.Service.Auth;
using Direnix.Service.Configuration;

namespace Direnix.Service.Endpoints;

/// <summary>
/// Gestão de usuários locais do portal (apenas LocalAdmin). Modelo de 2 papéis
/// exposto na UI: administrador (LocalAdmin) e somente leitura (ReadOnlyTechnical).
/// Não se aplica ao modo portátil (sessão local única, sem login).
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
            var (forbidden, selfId) = await RequireAdminAsync(store, http, portable, ct);
            if (forbidden is not null) return forbidden;

            var now = DateTimeOffset.UtcNow;
            var users = await store.ListUsersAsync(ct);
            var online = (await store.ListActiveSessionUsernamesAsync(now, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return Results.Ok(new
            {
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
            var (forbidden, _) = await RequireAdminAsync(store, http, portable, ct);
            if (forbidden is not null) return forbidden;

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
            var (forbidden, _) = await RequireAdminAsync(store, http, portable, ct);
            if (forbidden is not null) return forbidden;

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
            if (role != Admin && await IsLastAdminAsync(store, target, ct))
            {
                return Results.BadRequest(new { error = "Deve existir ao menos um administrador." });
            }
            await store.UpdateUserRoleAsync(userId, role, ct);
            await PortalAudit.LogAsync(store, http, "UserRoleChanged", "User", $"{target.Username} -> {role}", "Success");
            return Results.Ok(new { userId, role });
        });

        group.MapPut("/{userId}/password", async (string userId, UserPasswordBody body, IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct) =>
        {
            var (forbidden, _) = await RequireAdminAsync(store, http, portable, ct);
            if (forbidden is not null) return forbidden;

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
            var (forbidden, selfId) = await RequireAdminAsync(store, http, portable, ct);
            if (forbidden is not null) return forbidden;

            if (userId == selfId)
            {
                return Results.BadRequest(new { error = "Voce nao pode excluir a propria conta." });
            }
            var target = await store.GetUserByIdAsync(userId, ct);
            if (target is null)
            {
                return Results.NotFound(new { error = "Usuario nao encontrado." });
            }
            if (await IsLastAdminAsync(store, target, ct))
            {
                return Results.BadRequest(new { error = "Deve existir ao menos um administrador." });
            }
            await store.DeleteUserAsync(userId, ct);
            await PortalAudit.LogAsync(store, http, "UserDeleted", "User", target.Username, "Success");
            return Results.Ok(new { deleted = userId });
        });

        return endpoints;
    }

    // Gate único de todos os handlers: bloqueia o modo portátil (não há multiusuário)
    // e exige LocalAdmin. Retorna o 403 pronto (ou null) + o UserId real do próprio
    // usuário (para isSelf / excluir-self).
    private static async Task<(IResult? forbidden, string? selfId)> RequireAdminAsync(
        IProductStore store, HttpContext http, PortableModeState portable, CancellationToken ct)
    {
        if (portable.IsPortable)
        {
            return (Results.Json(new { error = "Gestao de usuarios nao se aplica ao modo portatil." }, statusCode: 403), null);
        }
        var user = await AuthEndpoints.ResolveActingUserAsync(store, http, ct);
        if (!AuthEndpoints.IsAdmin(user))
        {
            return (Results.Json(new { error = "Apenas administradores podem gerenciar usuarios." }, statusCode: 403), null);
        }
        return (null, user!.UserId);
    }

    // Não deixar a conta ficar sem nenhum administrador.
    private static async Task<bool> IsLastAdminAsync(IProductStore store, AppUserRecord target, CancellationToken ct) =>
        target.Role == Admin && await store.CountUsersByRoleAsync(Admin, ct) <= 1;

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
