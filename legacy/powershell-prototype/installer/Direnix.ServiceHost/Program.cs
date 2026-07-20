using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;

namespace Direnix.Service
{
    internal static class Program
    {
        private const string ServiceName = "DirenixPortal";

        private static int Main(string[] args)
        {
            if (Environment.UserInteractive || HasArg(args, "--console"))
            {
                ServiceRuntime.RunConsole(args);
                return 0;
            }

            ServiceBase.Run(new PortalService(args));
            return 0;
        }

        private static bool HasArg(string[] args, string value)
        {
            foreach (string arg in args)
            {
                if (string.Equals(arg, value, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private sealed class PortalService : ServiceBase
        {
            private readonly string[] args;

            public PortalService(string[] args)
            {
                this.args = args;
                ServiceName = Program.ServiceName;
                CanStop = true;
                CanShutdown = true;
            }

            protected override void OnStart(string[] startArgs)
            {
                ServiceRuntime.Start(args);
            }

            protected override void OnStop()
            {
                ServiceRuntime.Stop();
            }

            protected override void OnShutdown()
            {
                ServiceRuntime.Stop();
            }
        }

        private static class ServiceRuntime
        {
            private static Process portalProcess;
            private static string logRoot;

            public static void RunConsole(string[] args)
            {
                Start(args);
                Console.WriteLine("Direnix service host running. Press Enter to stop.");
                Console.ReadLine();
                Stop();
            }

            public static void Start(string[] args)
            {
                string configPath = GetArg(args, "--config");
                if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
                {
                    throw new FileNotFoundException("Direnix config was not found.", configPath);
                }

                InstallConfig config = InstallConfig.Load(configPath);
                logRoot = config.LogRoot;
                Directory.CreateDirectory(logRoot);

                string script = Path.Combine(config.InstallRoot, "scripts", "Start-DirenixPortal.ps1");
                if (!File.Exists(script))
                {
                    throw new FileNotFoundException("Direnix portal script was not found.", script);
                }

                string stdout = Path.Combine(logRoot, "portal-service.out.log");
                string stderr = Path.Combine(logRoot, "portal-service.err.log");
                string arguments =
                    "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden " +
                    "-File " + Quote(script) + " " +
                    "-Port " + config.Port + " " +
                    "-ListenAddress " + Quote(config.ListenAddress) + " " +
                    "-DataRoot " + Quote(config.DataRoot) + " " +
                    "-OutputRoot " + Quote(config.OutputRoot) + " " +
                    "-ConfigPath " + Quote(configPath);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = arguments;
                psi.WorkingDirectory = config.InstallRoot;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.EnvironmentVariables["DIRENIX_DPAPI_SCOPE"] = "LocalMachine";
                psi.EnvironmentVariables["DIRENIX_DATA_ROOT"] = config.DataRoot;
                psi.EnvironmentVariables["DIRENIX_OUTPUT_ROOT"] = config.OutputRoot;
                psi.EnvironmentVariables["DIRENIX_CONFIG_PATH"] = configPath;

                portalProcess = new Process();
                portalProcess.StartInfo = psi;
                portalProcess.EnableRaisingEvents = true;
                portalProcess.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null) AppendLine(stdout, e.Data);
                };
                portalProcess.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data != null) AppendLine(stderr, e.Data);
                };
                portalProcess.Exited += delegate
                {
                    AppendLine(stderr, "Portal process exited with code " + portalProcess.ExitCode);
                };

                AppendLine(stdout, "Starting Direnix portal service host at " + DateTimeOffset.Now);
                portalProcess.Start();
                portalProcess.BeginOutputReadLine();
                portalProcess.BeginErrorReadLine();
            }

            public static void Stop()
            {
                try
                {
                    if (portalProcess != null && !portalProcess.HasExited)
                    {
                        portalProcess.Kill();
                        portalProcess.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrWhiteSpace(logRoot))
                    {
                        AppendLine(Path.Combine(logRoot, "portal-service.err.log"), ex.ToString());
                    }
                }
            }

            private static string GetArg(string[] args, string name)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        return args[i + 1];
                    }
                }
                return string.Empty;
            }

            private static string Quote(string value)
            {
                return "\"" + value.Replace("\"", "\\\"") + "\"";
            }

            private static void AppendLine(string path, string line)
            {
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        private sealed class InstallConfig
        {
            public string InstallRoot;
            public string DataRoot;
            public string OutputRoot;
            public string LogRoot;
            public string ListenAddress;
            public int Port;

            public static InstallConfig Load(string path)
            {
                string json = File.ReadAllText(path);
                return new InstallConfig
                {
                    InstallRoot = ReadJsonString(json, "installRoot"),
                    DataRoot = ReadJsonString(json, "dataRoot"),
                    OutputRoot = ReadJsonString(json, "outputRoot"),
                    LogRoot = ReadJsonString(json, "logRoot"),
                    ListenAddress = ReadJsonString(json, "listenAddress"),
                    Port = ReadJsonInt(json, "port")
                };
            }

            private static string ReadJsonString(string json, string name)
            {
                string marker = "\"" + name + "\"";
                int key = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (key < 0) return string.Empty;
                int colon = json.IndexOf(':', key);
                int start = json.IndexOf('"', colon + 1);
                int end = start + 1;
                bool escape = false;
                StringBuilder builder = new StringBuilder();
                while (end < json.Length)
                {
                    char ch = json[end++];
                    if (escape)
                    {
                        if (ch == 'n') builder.Append('\n');
                        else if (ch == 'r') builder.Append('\r');
                        else if (ch == 't') builder.Append('\t');
                        else builder.Append(ch);
                        escape = false;
                    }
                    else if (ch == '\\')
                    {
                        escape = true;
                    }
                    else if (ch == '"')
                    {
                        break;
                    }
                    else
                    {
                        builder.Append(ch);
                    }
                }
                return builder.ToString();
            }

            private static int ReadJsonInt(string json, string name)
            {
                string marker = "\"" + name + "\"";
                int key = json.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (key < 0) return 0;
                int colon = json.IndexOf(':', key);
                int start = colon + 1;
                while (start < json.Length && char.IsWhiteSpace(json[start])) start++;
                int end = start;
                while (end < json.Length && char.IsDigit(json[end])) end++;
                return int.Parse(json.Substring(start, end - start));
            }
        }
    }
}
