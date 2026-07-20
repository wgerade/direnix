namespace Direnix.Core.Auth;

/// <summary>Usuário local do portal (login mínimo).</summary>
public sealed record AppUserRecord(
    string UserId,
    string Username,
    string PasswordHash,
    string Salt,
    int Iterations,
    string Role,
    DateTimeOffset CreatedAt);

/// <summary>Sessão autenticada (cookie HttpOnly).</summary>
public sealed record AppSession(
    string Token,
    string UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
