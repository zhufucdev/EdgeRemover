using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Windows.Forms;

namespace EdgeRemover
{
    internal class Program
    {
        private const string EDGE_PROTOCOL = "microsoft-edge:?";
        private const string IFEO_PATH = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe";
        private const string EDGE_EXE = "msedge.exe";
        private const string ER_FLAG = "--edge-remover";
        private static readonly string[] IGNORED_FLAGS = new string[] { "--single-argument" };

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                if (!IsUserAdmin())
                {
                    RelaunchAsAdmin();
                }
                Register();
            }
            else
            {
                string url, extra;

                var query = args.FirstOrDefault(v => v.StartsWith(EDGE_PROTOCOL));
                if (query != null)
                {
                    var index = query.IndexOf("url=");
                    if (index < 0)
                    {
                        MessageBox.Show(string.Format("Unexpected input: {0}", query), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                    }

                    var indexEnd = query.IndexOf('&', index + 1);
                    if (indexEnd < 0)
                    {
                        indexEnd = query.Length;
                    }
                    url = HttpUtility.UrlDecode(query.Substring(index + 4, indexEnd - index - 4));
                    extra = "";
                }
                else if (args[0].Contains(EDGE_EXE))
                {
                    url = string.Empty;
                    extra = string.Empty;
                    if (args.Count() <= 1)
                    {
                        // this is a normal call
                        MessageBox.Show("Microsoft Edge won't be called in your system.", "EdgeRemover", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Environment.Exit(1);
                    }
                    else if (args.Contains(ER_FLAG) || DateTime.Now - Settings.Default.LastCall < TimeSpan.FromSeconds(1))
                    {
                        // this is a loopback
                        MessageBox.Show("This file is set to be opened with Edge, which is blocked.\nTry another app.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                    }
                    else
                    {
                        for (var i = 1; i < args.Length; i++)
                        {
                            if (IGNORED_FLAGS.Contains(args[i]))
                            {
                                continue;
                            }
                            if (url == string.Empty)
                            {
                                url = args[i];
                            }
                            else
                            {
                                extra += args[i] + " ";
                            }
                        }
                    }
                } 
                else
                {
                    extra = string.Empty;
                    url = args[0];
                    for (var i = 1; i < args.Length; i++)
                    {
                        extra += args[i] + " ";
                    }
                }
                Open(url, extra);
            }
        }

        static void Open(string file, string extraArg)
        {
            Settings.Default.LastCall = DateTime.Now;
            Settings.Default.Save();

            var process = new ProcessStartInfo()
            {
                FileName = file,
                Arguments = extraArg
            };
            Process.Start(process);
        }

        static bool IsUserAdmin()
        {
            bool isAdmin = false;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (Exception)
            {
            }
            return isAdmin;
        }

        static void RelaunchAsAdmin()
        {
            Console.WriteLine("This app is invoked by user acively.");
            Console.WriteLine("==> Will register itself in Image File Execution Options registry.");
            ProcessStartInfo proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Assembly.GetEntryAssembly().CodeBase,
                Verb = "runas"
            };
            try
            {
                Process.Start(proc);
                Environment.Exit(0);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "This program must be run as an administrator this time!",
                    "Registration failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        static void Register()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(IFEO_PATH))
                {
                    key.SetValue("Debugger", Assembly.GetExecutingAssembly().Location);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Environment.Exit(1);
            }
            MessageBox.Show(string.Format(@"See Computer\HKEY_LOCAL_MACHINE\{0} for details.", IFEO_PATH), "Registration successful");
        }

    }
}
