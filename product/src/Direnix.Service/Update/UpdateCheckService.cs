using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Direnix.Core.Storage;
using Direnix.Service.Reporting;

namespace Direnix.Service.Update;

/// <summary>Estado do check de atualização exposto à UI.</summary>
public sealed record UpdateStatus(
    bool Enabled,
    string Current,
    string? Latest,
    bool UpdateAvailable,
    string ReleaseUrl,
    DateTimeOffset? CheckedAt,
    string? Note);

/// <summary>
/// Verifica se há release mais nova no GitHub. Desligado por padrão para preservar
/// a promessa de zero egress: NENHUMA chamada de rede acontece sem o usuário ativar
/// (ou pedir um check manual, que é consentimento explícito). Só lê a versão mais
/// recente — nenhum dado é enviado. Falha de rede é silenciosa.
/// </summary>
public sealed class UpdateCheckService
{
    private const string EnabledKey = "update_check_enabled";
    private const string CacheKey = "update_check_cache";
    private const string ReleasesApi = "https://api.github.com/repos/wgerade/direnix/releases/latest";
    private const string ReleasesPage = "https://github.com/wgerade/direnix/releases/latest";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IProductStore store;
    private readonly HttpClient http;
    private readonly ILogger<UpdateCheckService> logger;

    public UpdateCheckService(IProductStore store, HttpClient http, ILogger<UpdateCheckService> logger)
    {
        this.store = store;
        this.http = http;
        this.logger = logger;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken cancellationToken) =>
        string.Equals(await store.GetSettingAsync(EnabledKey, cancellationToken), "true", StringComparison.OrdinalIgnoreCase);

    public async Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken) =>
        await store.SetSettingAsync(EnabledKey, enabled ? "true" : "false", cancellationToken);

    /// <summary>
    /// Retorna o status. <paramref name="force"/> = check manual iniciado pelo usuário
    /// (bate na rede mesmo desligado). Sem force: só consulta a rede se estiver ligado
    /// e o cache tiver expirado; caso contrário devolve cache/estado local sem egress.
    /// </summary>
    public async Task<UpdateStatus> GetStatusAsync(bool force, CancellationToken cancellationToken)
    {
        var current = ReportModelBuilder.ProductVersion();
        var enabled = await IsEnabledAsync(cancellationToken);
        var cached = await ReadCacheAsync(cancellationToken);

        // Sem egress: desligado e sem pedido manual → devolve o que houver localmente.
        if (!enabled && !force)
        {
            return new UpdateStatus(false, current, cached?.Latest, false, ReleasesPage, cached?.CheckedAt, null);
        }

        // Ligado, cache ainda válido e não forçado → reaproveita (1x/dia).
        if (!force && cached is not null && DateTimeOffset.UtcNow - cached.CheckedAt < CacheTtl)
        {
            return Build(current, cached.Latest, enabled, cached.CheckedAt);
        }

        var latest = await FetchLatestAsync(cancellationToken);
        if (latest is null)
        {
            // Falha silenciosa (offline, rate limit): mantém cache anterior, se houver.
            return new UpdateStatus(enabled, current, cached?.Latest,
                IsNewer(cached?.Latest, current), ReleasesPage, cached?.CheckedAt,
                "check-failed");
        }

        var now = DateTimeOffset.UtcNow;
        await WriteCacheAsync(new CacheEntry(latest, now), cancellationToken);
        return Build(current, latest, enabled, now);
    }

    private static UpdateStatus Build(string current, string? latest, bool enabled, DateTimeOffset? checkedAt) =>
        new(enabled, current, latest, IsNewer(latest, current), ReleasesPage, checkedAt, null);

    private async Task<string?> FetchLatestAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApi);
            request.Headers.UserAgent.ParseAdd("Direnix-update-check");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken: cancellationToken);
            var tag = release?.TagName?.TrimStart('v', 'V');
            return string.IsNullOrWhiteSpace(tag) ? null : tag;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException or JsonException)
        {
            logger.LogDebug(ex, "Check de atualizacao falhou (silencioso).");
            return null;
        }
    }

    /// <summary>Compara versões semânticas; true se latest &gt; current.</summary>
    internal static bool IsNewer(string? latest, string current)
    {
        if (string.IsNullOrWhiteSpace(latest))
        {
            return false;
        }

        return Version.TryParse(Normalize(latest), out var l)
            && Version.TryParse(Normalize(current), out var c)
            && l > c;
    }

    // Descarta sufixos de pré-lançamento/build (0.9.0-dev, 1.0.0+abc) para o Version.Parse.
    private static string Normalize(string value)
    {
        var cut = value.IndexOfAny(new[] { '-', '+' });
        return cut > 0 ? value[..cut] : value;
    }

    private async Task<CacheEntry?> ReadCacheAsync(CancellationToken cancellationToken)
    {
        var json = await store.GetSettingAsync(CacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try { return JsonSerializer.Deserialize<CacheEntry>(json, JsonOptions); }
        catch (JsonException) { return null; }
    }

    private async Task WriteCacheAsync(CacheEntry entry, CancellationToken cancellationToken) =>
        await store.SetSettingAsync(CacheKey, JsonSerializer.Serialize(entry, JsonOptions), cancellationToken);

    private sealed record CacheEntry(string Latest, DateTimeOffset CheckedAt);

    private sealed record GitHubRelease([property: JsonPropertyName("tag_name")] string? TagName);
}
