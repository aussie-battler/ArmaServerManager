using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace ArmaServerManager
{

    public class Program
    {

        public static void Main(string[] args)
        {

            ServerManager.LoadServerList();
            HttpServer s = new HttpServer(SettingsManager.LoadSettings());
            s.Listen();




            /*SrvProcPair srv1 = ServerManager.CreateNewServer();

            srv1.serverData.InsertSubClass(srv1.serverData.Missions, new Arma3MissionClass("Mission_1", "A3wasteland_stratis", "Custom"));

            Arma3ServerConfigWriter.WriteConfigFile(srv1.serverData, SettingsManager.LoadSettings());

            string serverDataString = ServerManager.GetServerDataByID(0);
            Console.WriteLine(serverDataString);

            File.WriteAllText("File.txt", serverDataString);*/


             /*Arma3Server server = new Arma3Server();
            server.ServerID = 600;
            server.GamePort = 6666;
            server.AdminPassword.paramValue = "esso";
            server.AdminPassword.include = true;

            server.SetArrayParameter(new string[]{"Welcome to the server", "Join us on essobäric.com"}, server.ServerMOTD);
            server.ServerMOTD.include = true;

            server.Missions.SubClasses.Add(new Arma3MissionClass("Mission_1", "A3Wasteland_v1.2.Stratis","Custom"));

            Arma3ServerConfigWriter.WriteConfigFile(server, SettingsManager.LoadSettings());

            SrvProcPair pair = new SrvProcPair();

            string RelativePath = s.ArmaServersDataPath + "/" + server.ServerID;
            Process p = Process.Start(s.Arma3ServerExePath, "-port="+server.GamePort + " -config="+RelativePath+"/serverconfig.cfg" + " -profiles="+RelativePath + " -name="+server.ServerProfileName+" -bepath="+s.BattlEyePath);

            pair.proc = p;
            pair.serverData = server;

            Console.WriteLine("Started: {0} with settings of {1}", pair.proc.Id.ToString(), pair.serverData.ToString());

            //Thread.Sleep(30000);

           // pair.proc.Kill();*/

            Console.Read();
        }
    }
}
