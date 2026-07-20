using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Direnix.Setup
{
    internal static class Program
    {
        [STAThread]
        private static int Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string root = FindPackageRoot(AppDomain.CurrentDomain.BaseDirectory);
                if (root == null)
                {
                    MessageBox.Show(
                        "Nao foi possivel localizar scripts\\Start-DirenixSetupWizard.ps1. Mantenha o instalador dentro do pacote Direnix.",
                        "Direnix Setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return 2;
                }

                string wizard = Path.Combine(root, "scripts", "Start-DirenixSetupWizard.ps1");
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -STA -File \"" + wizard + "\"";
                psi.WorkingDirectory = root;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                Process process = Process.Start(psi);
                if (process == null)
                {
                    MessageBox.Show("Nao foi possivel iniciar o wizard.", "Direnix Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 3;
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Direnix Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static string FindPackageRoot(string startDirectory)
        {
            DirectoryInfo directory = new DirectoryInfo(startDirectory);
            while (directory != null)
            {
                string wizard = Path.Combine(directory.FullName, "scripts", "Start-DirenixSetupWizard.ps1");
                string portal = Path.Combine(directory.FullName, "scripts", "Start-DirenixPortal.ps1");
                string dashboard = Path.Combine(directory.FullName, "dashboard", "index.html");
                if (File.Exists(wizard) && File.Exists(portal) && File.Exists(dashboard))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            string current = Directory.GetCurrentDirectory();
            string currentWizard = Path.Combine(current, "scripts", "Start-DirenixSetupWizard.ps1");
            if (File.Exists(currentWizard))
            {
                return current;
            }

            return null;
        }
    }
}
