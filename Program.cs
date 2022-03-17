using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace EdgeRemover
{
    internal class Program
    {
        private const string EDGE_PROTOCOL = "microsoft-edge:?";
        private const string IFEO_PATH = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\msedge.exe";

        private static string ConfigurationPath;

        static void Main(string[] args)
        {
            var query = args.FirstOrDefault(v => v.StartsWith(EDGE_PROTOCOL));
            if (query == null)
            {
                if (!IsUserAdmin())
                {
                    RelaunchAsAdmin();
                }
                Register();
            }
            else
            {
                var index = query.IndexOf("url=");
                if (index < 0)
                {
                    Console.WriteLine(string.Format("Unexpected input: {0}", query));
                    PreExit(1);
                }

                var indexEnd = query.IndexOf('&', index + 1);
                if (indexEnd < 0)
                {
                    indexEnd = query.Length;
                }
                var url = HttpUtility.UrlDecode(query.Substring(index + 4, indexEnd - index - 4));
                var process = new ProcessStartInfo()
                {
                    FileName = url
                };
                Process.Start(process);
            }
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
            catch (Exception ex)
            {
                Console.WriteLine("Error: This program must be run as an administrator! \n\n" + ex.ToString());
                PreExit(1);
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
                PreExit(1);
            }
            Console.WriteLine("Registration successful.");
            Console.WriteLine(string.Format(@"See Computer\HKEY_LOCAL_MACHINE\{0} for details.", IFEO_PATH));
            PreExit();
        }

        static void PreExit(int State = 0)
        {
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            Environment.Exit(State);
        }
    }
}
