using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.IO;
using ArmaServerManager.A3S;
using System.Security.Principal;
using System.Windows.Forms;

namespace ArmaServerManager
{

    public class Program
    {

        public static void Main(string[] args)
        {

            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            if (!isElevated)
            {
                MessageBox.Show("Admin permissions required.", "No Privileges");
                Environment.Exit(0);
            }
            

            Arma3ServerUtility.LoadServerList();
            
            HttpServer s = new HttpServer(SettingsManager.LoadSettings());
            s.Listen();
        }
    }
} 
