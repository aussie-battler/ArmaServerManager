using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArmaServerManager;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArmaServerManager.A3S
{
    public class Arma3ServerUtility
    {
        //Load generic application settings.
        private static Settings appSettings = SettingsManager.LoadSettings();

        //List of ServerProcessPairs. Servers will be stored here. Servers will be loaded here on Application Start.
        public static List<SrvProcPair> ServerList = new List<SrvProcPair>();

        public static void ServerListAppend(SrvProcPair serverProcessPair)
        {
            ServerList.Add(serverProcessPair);
            //Save serverlist
        }
        public static void ServerListRemove(SrvProcPair serverProcessPair)
        {
            ServerList.Remove(serverProcessPair);
            //save serverlist
        }
        public static void ServerListRemove(int index)
        {
            ServerList.RemoveAt(index);
            //save serverlist
        }



        //Start Arma 3 Server Executable.
        public static bool StartServer(SrvProcPair serverProcessPair, out string result)
        {
            return Arma3ServerUtility.StartServer(serverProcessPair, appSettings.BattlEyePath, out result);
        }
        public static bool StartServer(SrvProcPair serverProcessPair, string battlEyePath, out string result)
        {
            try
            {
                Arma3Server server = serverProcessPair.serverData;
                if (server == null)
                {
                    result = "Server is not defined";
                    return false;
                }

                //Refresh arma3server config file before restarting server.
                Arma3ServerConfigWriter.WriteConfigFile(server, appSettings);

                string RelativePath = appSettings.ArmaServersDataPath + '\\' + server.ServerID;

                Process p = Process.Start(appSettings.Arma3ServerExePath, "\"-port=" + server.GamePort + "\" \"-config=" + RelativePath + "\\serverconfig.cfg\"" + " \"-profiles=" + RelativePath + "\" \"-name=" + server.ServerProfileName + "\" \"-bepath=" + battlEyePath + "\"");
                serverProcessPair.proc = p;
                result = "Process started";
                return true;
            }
            catch (Exception e)
            {
                result = "Could not start process: " + e.Message;
                return false;
            }
        }

        //Stop Arma 3 server Executable.
        public static bool StopServer(SrvProcPair serverProcessPair, out string result)
        {
            try
            {
                serverProcessPair.proc.Kill();
                serverProcessPair.proc.WaitForExit();

                result = "Process Stopped";
                return true;
            }
            catch (Exception e)
            {
                result = "Could not stop server: " + e.Message;
                return false;
            }
        }

        //Restart Arma 3 Server Executable.
        public static bool RestartServer(SrvProcPair serverProcessPair, out string result)
        {
            StopServer(serverProcessPair, out result);
            return StartServer(serverProcessPair, out result);
        }


        //Create new Arma 3 Server and add to ServerList. Return id of newly created server.
        public static int CreateNewServer()
        {
            Arma3Server server = new Arma3Server();
            server.ServerID = FindFreeID();

            SrvProcPair serverProcessPair = new SrvProcPair();
            serverProcessPair.serverData = server;
            ServerListAppend(serverProcessPair);

            return server.ServerID;
        }
        
        //Find unique ID for Arma 3 Server.
        private static int FindFreeID()
        {
            int id = 0;
            while (true)
            {
                bool taken = false;
                foreach (var item in ServerList)
                {
                    if (item.serverData.ServerID == id) taken = true;
                }
                if (!taken)
                    return id;
                id++;
            }
        }


        //Find Arma 3 Server by ID.
        public static Arma3Server FindArma3ServerByID(int id)
        {
            SrvProcPair serverProcessPair = ServerList.Find(x => x.serverData.ServerID == id);
            if (serverProcessPair == null) return null;
            return serverProcessPair.serverData;
        }

        //Find ServerProcessPair by serverID.
        public static SrvProcPair FindServerProcessPairByID(int id)
        {
            return ServerList.Find(x => x.serverData.ServerID == id);
        }


        //Save ServerList
        public static void SaveServerList()
        {
            try
            {
                List<ServerListStorageObject> list = new List<ServerListStorageObject>();

                foreach (var item in ServerList)
                {
                    list.Add(new ServerListStorageObject(item.proc, item.serverData, item.serverData.Schedules.ServerEvents));
                }

                if (File.Exists("serversave")) File.Delete("serversave");

                FileStream stream = File.Create("serversave");
                var formatter = new BinaryFormatter();
                formatter.Serialize(stream, list);
                stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while saving serverlist: {0}", e.Message);
            }
            
        }

        //LoadServerList spagethi.
        public static void LoadServerList()
        {
            if (File.Exists("serversave"))
            {
                try
                {
                    FileStream stream = File.OpenRead("serversave");
                    var formatter = new BinaryFormatter();
                    List<ServerListStorageObject> list = formatter.Deserialize(stream) as List<ServerListStorageObject>;
                    stream.Close();

                    foreach (var item in list)
                    {
                        Process p = null;
                        try
                        {
                            p = Process.GetProcessById(item.ProcessID);
                        }
                        catch (Exception) { }

                        if (p != null)
                        {
                            if (p.ProcessName != item.ProcessName)
                                p = null;
                        }
                        SrvProcPair spp = new SrvProcPair();
                        spp.serverData = item.ServerData;
                        spp.proc = p;
                        spp.serverData.Schedules = new ServerSchedule();
                        spp.serverData.Schedules.ServerEvents = item.Events;
                        ServerList.Add(spp);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to load serverlist: {0}", e.Message);
                }
               
            }
        }

    }
}
