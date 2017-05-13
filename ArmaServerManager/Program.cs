using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.IO;
using ArmaServerManager.A3S;

namespace ArmaServerManager
{

    public class Program
    {

        public static void Main(string[] args)
        {
            Arma3ServerUtility.LoadServerList();
            
            HttpServer s = new HttpServer(SettingsManager.LoadSettings());
            s.Listen();



            /*SrvProcPair srv1 = ServerManager.CreateNewServer();

            srv1.serverData.InsertSubClass(srv1.serverData.Missions, new Arma3MissionClass("Mission_1", "A3wasteland_stratis", "Custom"));

            Arma3ServerConfigWriter.WriteConfigFile(srv1.serverData, SettingsManager.LoadSettings());

            */
        }
    }
}
