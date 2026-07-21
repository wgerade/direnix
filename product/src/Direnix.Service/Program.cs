using System.Net;
using Direnix.Core.Collection;
using Direnix.Core.Rules;
using Direnix.Core.Storage;
using Direnix.Infrastructure.Directory;
using Direnix.Infrastructure.Storage;
using Direnix.Service.Auth;
using Direnix.Service.Configuration;
using Direnix.Service.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "Direnix.Service";
});

builder.Services.Configure<ProductHostOptions>(
    builder.Configuration.GetSection(ProductHostOptions.SectionName));

var hostOptions = builder.Configuration
    .GetSection(ProductHostOptions.SectionName)
    .Get<ProductHostOptions>() ?? new ProductHostOptions();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Parse(hostOptions.ListenAddress), hostOptions.Port);
});

var storageOptions = builder.Configuration
    .GetSection(ProductStorageOptions.SectionName)
    .Get<ProductStorageOptions>() ?? new ProductStorageOptions();
builder.Services.AddSingleton(storageOptions);
builder.Services.AddSingleton<IDatabaseKeyStore, WindowsDpapiDatabaseKeyStore>();
builder.Services.AddSingleton<SqlCipherProductStore>();
builder.Services.AddSingleton<IProductStore>(provider => provider.GetRequiredService<SqlCipherProductStore>());
builder.Services.AddSingleton<ISchemaMigrator>(provider => provider.GetRequiredService<SqlCipherProductStore>());
builder.Services.AddSingleton<IAdDirectoryProbe, LdapDirectoryProbe>();
builder.Services.AddSingleton<ICollectionEngine, LdapCollector>();
builder.Services.AddSingleton<HygieneRuleEngine>();
builder.Services.AddSingleton<Direnix.Service.Collection.CollectionJobService>();

// Relatório/digest: fonte única do ReportModel + canais de notificação.
builder.Services.AddSingleton<Direnix.Service.Reporting.ReportModelBuilder>();
builder.Services.AddSingleton<Direnix.Core.Notifications.ISecretProtector, Direnix.Infrastructure.Notifications.WindowsDpapiSecretProtector>();
builder.Services.AddSingleton<Direnix.Infrastructure.Notifications.SmtpDigestSender>();
builder.Services.AddSingleton(_ => new Direnix.Infrastructure.Notifications.WebhookDigestSender(
    new HttpClient { Timeout = TimeSpan.FromSeconds(15) }));
builder.Services.AddSingleton<Direnix.Service.Notifications.NotificationService>();

builder.Services.AddHostedService<Direnix.Service.Collection.ScheduledCollectionService>();

// Serializa enums como string em toda a API (evita expor valores numericos
// que quebram o frontend, ex.: state/status).
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));

var app = builder.Build();

// Aplica migrations uma vez no startup (em vez de a cada health check).
using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<ISchemaMigrator>();
    await migrator.MigrateAsync(CancellationToken.None);
}

// Modo CLI: reset seguro do admin (reabre "Criar administrador" sem apagar coletas).
// Uso (prompt elevado): Direnix.Service.exe --reset-admin
if (args.Any(a => string.Equals(a, "--reset-admin", StringComparison.OrdinalIgnoreCase)))
{
    using var scope = app.Services.CreateScope();
    var store = scope.ServiceProvider.GetRequiredService<IProductStore>();
    var removed = await store.ResetAdminAsync(CancellationToken.None);
    Console.WriteLine($"[reset-admin] {removed} usuario(s) removido(s). Abra http://127.0.0.1:8787 e crie um novo administrador.");
    return;
}

// O portal é servido localmente e itera rápido. Assets estáticos (js/css)
// trazem ?v=<versão> no index e podem ser cacheados; já o próprio index.html
// precisa revalidar sempre, senão um upgrade entrega a UI antiga com os
// script tags velhos (sem a query de versão) e o cache-busting nunca aplica.
// As mesmas opções valem para o fallback do SPA (MapFallbackToFile), que também
// entrega index.html.
var staticFileOptions = new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        }
    }
};

app.UseDefaultFiles();
app.UseStaticFiles(staticFileOptions);

// Gate de autenticacao: bloqueia chamadas que MUDAM estado (POST/PUT/DELETE em /api)
// sem sessao valida. Endpoints de auth ficam liberados para bootstrap/login.
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    var method = context.Request.Method;
    var isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    var isMutating = HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method);
    var isAuthEndpoint = path.StartsWith("/api/v1/auth/", StringComparison.OrdinalIgnoreCase);

    if (isApi && isMutating && !isAuthEndpoint)
    {
        var store = context.RequestServices.GetRequiredService<IProductStore>();
        var session = await Direnix.Service.Auth.AuthEndpoints.ResolveSessionAsync(store, context, context.RequestAborted);
        if (session is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Autenticacao necessaria." });
            return;
        }
    }

    await next();
});

app.MapHealthEndpoints();
app.MapSystemEndpoints();
app.MapProductApplicationEndpoints();
app.MapCollectionEndpoints();
app.MapFindingsEndpoints();
app.MapReportEndpoints();
app.MapAuthEndpoints();
app.MapScheduleEndpoints();
app.MapNotificationEndpoints();
app.MapFallbackToFile("index.html", staticFileOptions);

app.Run();
