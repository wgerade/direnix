namespace Direnix.Core.Auth;

/// <summary>Usuário local do portal (login + segurança básica).</summary>
public sealed record AppUserRecord(
    string UserId,
    string Username,
    string PasswordHash,
    string Salt,
    int Iterations,
    string Role,
    DateTimeOffset CreatedAt,
    int FailedAttempts = 0,
    DateTimeOffset? LockedUntil = null,
    DateTimeOffset? LastLogin = null)
{
    /// <summary>A conta está bloqueada por força bruta agora?</summary>
    public bool IsLockedNow(DateTimeOffset now) => LockedUntil is { } until && until > now;
}

/// <summary>Sessão autenticada (cookie HttpOnly).</summary>
public sealed record AppSession(
    string Token,
    string UserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
