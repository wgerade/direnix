using System.Diagnostics;
using Microsoft.Win32;

namespace Direnix.Service.Configuration;

/// <summary>
/// Lê e aplica a configuração do PRÓPRIO serviço Windows (tipo de inicialização e
/// conta de logon) via registro + sc.exe. Para gMSA, a conta é informada SEM senha
/// (o Windows gerencia/rotaciona). Requer o serviço rodando com privilégio (caso
/// típico: LocalSystem ou conta administrativa).
/// </summary>
public static class WindowsServiceControl
{
    public const string ServiceName = "Direnix.Service";
    private const string RegPath = @"SYSTEM\CurrentControlSet\Services\" + ServiceName;

    public sealed record ServiceStatus(bool Installed, string Identity, string StartupType);

    public static ServiceStatus GetStatus()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(RegPath);
            if (key is null)
            {
                return new ServiceStatus(false, "—", "—");
            }

            var identity = key.GetValue("ObjectName") as string;
            if (string.IsNullOrWhiteSpace(identity)) identity = "LocalSystem";

            var start = Convert.ToInt32(key.GetValue("Start") ?? 3);
            var delayed = Convert.ToInt32(key.GetValue("DelayedAutostart") ?? 0);
            var startup = start switch
            {
                2 => delayed == 1 ? "AutomaticDelayed" : "Automatic",
                3 => "Manual",
                4 => "Disabled",
                _ => "Other"
            };

            return new ServiceStatus(true, identity, startup);
        }
        catch (Exception)
        {
            return new ServiceStatus(false, "—", "—");
        }
    }

    public sealed record ApplyResult(bool Ok, string Message);

    public static ApplyResult Apply(string startupType, string identityMode, string? accountName)
    {
        if (!GetStatus().Installed)
        {
            return new ApplyResult(false, "Servico nao instalado (configure pelo instalador/sc.exe).");
        }

        var scStart = startupType switch
        {
            "Automatic" => "auto",
            "AutomaticDelayed" => "delayed-auto",
            "Manual" => "demand",
            _ => "auto"
        };

        var (code1, out1) = RunSc("config", ServiceName, "start=", scStart);
        if (code1 != 0)
        {
            return new ApplyResult(false, $"Falha ao definir inicializacao: {out1.Trim()}");
        }

        if (string.Equals(identityMode, "Gmsa", StringComparison.OrdinalIgnoreCase))
        {
            var account = (accountName ?? string.Empty).Trim();
            if (account.Length == 0)
            {
                return new ApplyResult(false, "Informe a conta gMSA (ex.: DOMINIO\\svc-Direnix$).");
            }
            if (!account.EndsWith('$'))
            {
                account += "$"; // gMSA termina em '$'
            }

            var (code2, out2) = RunSc("config", ServiceName, "obj=", account, "password=", "");
            if (code2 != 0)
            {
                return new ApplyResult(false, $"Falha ao definir a conta gMSA: {out2.Trim()}");
            }
            return new ApplyResult(true, $"Conta '{account}' aplicada. Garanta 'Log on as a service' e reinicie o servico.");
        }

        var (code3, out3) = RunSc("config", ServiceName, "obj=", "LocalSystem");
        return code3 == 0
            ? new ApplyResult(true, "LocalSystem aplicado. Reinicie o servico para valer.")
            : new ApplyResult(false, $"Falha ao definir LocalSystem: {out3.Trim()}");
    }

    private static (int Code, string Output) RunSc(params string[] args)
    {
        var psi = new ProcessStartInfo("sc.exe")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        using var process = Process.Start(psi);
        if (process is null)
        {
            return (-1, "Nao foi possivel iniciar sc.exe.");
        }
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit(15000);
        return (process.ExitCode, output);
    }
}
