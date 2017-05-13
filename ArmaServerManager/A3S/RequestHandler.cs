using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.IO;

namespace ArmaServerManager.A3S
{
    public class RequestHandler
    {
        //Load generic application settings.
        private static Settings appSettings = SettingsManager.LoadSettings();


        //Update Server Config Parameter.
        private static string UpdateServerParam(List<string[]> request, int serverID)
        {
            string paramName, paramValue;
            if (!FindRequestValue(request, "param", out paramName) || !FindRequestValue(request, "paramvalue", out paramValue)) return "ParameterName or Value not found";
            return Arma3ServerData.UpdateConfigParam(serverID, paramName, paramValue);
        }

        //Update Server Config Parameter-State (included).
        private static string UpdateServerParamState(List<string[]> request, int serverID)
        {
            string paramName, paramValue;
            bool state;
            if (!FindRequestValue(request, "param", out paramName) || !FindRequestValue(request, "paramvalue", out paramValue)) return "ParameterName or Value not found";

            if(!bool.TryParse(paramValue, out state)) return "Value was not boolean-type.";

            return Arma3ServerData.UpdateConfigParamState(serverID, paramName, state);
        }

        //Update Server Game, Rcon or Query Port.
        private static string UpdatePort(List<string[]> request, int serverID, string portType)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string paramValue;
            int Port;

            if (!FindRequestValue(request, "paramvalue", out paramValue)) return "ParameterValue not found";
            if (!int.TryParse(paramValue, out Port)) return "Port was not Integer";
            if (Port < 0 || Port > 65535) return "Port value out of range";


            if (portType == "GamePort")
            {
                server.GamePort = Port;
                return "Updated GamePort to " + Port.ToString();
            }
            else if (portType == "QueryPort")
            {
                server.QueryParams.Port = Port;
                return "Updated QueryPort to " + Port.ToString();
            }
            else if (portType == "RconPort")
            {
                server.RconParams.Port = Port;
                return "Updated RconPort to " + Port.ToString();
            }

            return "PortType not found";
        }

        //Update Rcon or Query IP-Address.
        private static string UpdateIPAddress(List<string[]> request, int serverID, string addressType)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string paramValue;
            System.Net.IPAddress ipAddress;

            if (!FindRequestValue(request, "paramvalue", out paramValue)) return "ParameterValue not found";

            if (System.Net.IPAddress.TryParse(paramValue, out ipAddress))
            {
                if (addressType == "RconIP")
                {
                    server.RconParams.IPAddress = ipAddress.ToString();
                    return "Updater Rcon IP-Address to " + ipAddress.ToString();
                }

                if (addressType == "QueryIP")
                {
                    server.QueryParams.IPAddress = ipAddress.ToString();
                    return "Updater Query IP-Address to " + ipAddress.ToString();
                }

                return "IP-Address Target not found";
            }

            return "Invalid IP-Address String";
        }

        //Update Rcon Password.
        private static string UpdateRconPassword(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string paramValue;
            if (!FindRequestValue(request, "paramvalue", out paramValue)) return "ParameterValue not found";

            server.RconParams.Password = paramValue;
            return "Rcon Password Updated";
        }

        //Update Server Profilename.
        private static string UpdateServerProfileName(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string paramValue;
            if (!FindRequestValue(request, "paramvalue", out paramValue)) return "ParameterValue not found";

            server.ServerProfileName = paramValue;
            return "Server Profilename Updated";
        }

        //Get Arma3Server class to JSON string.
        private static string GetServerInfo(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string result;
            Arma3ServerData.ServerToJSON(server, out result);
            return result;
        }

        //Get Server Query class to JSON string.
        private static string GetQueryInfo(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            SourceQuery query = new SourceQuery();
            return new JavaScriptSerializer().Serialize(query.GetServerInfo(server.QueryParams.IPAddress, server.QueryParams.Port));
        }

        //Add mission to missioncycle.
        private static string AddMissionToCycle(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string name, file, difficulty;

            if (!FindRequestValue(request, "missionname", out name) || !FindRequestValue(request, "missionfile", out file) || !FindRequestValue(request, "difficulty", out difficulty))
                return "Mission Name, FileName or Difficulty not defined";

            if (name.Length > 0 && file.Length > 0 && difficulty.Length > 0) //
            {
                server.Missions.SubClasses.Add(new Arma3MissionClass(name, file, difficulty));
                return "New Mission Added.";
            }

            return "Mission Name, FileName or Difficulty not defined";
        }

        //Remove mission from missioncycle.
        private static string RemoveMissionFromCycle(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string param;
            if (!FindRequestValue(request, "param", out param)) return "Parameter not found";

            int removedCount = server.Missions.RemoveSubclassesByName(param);

            return "Removed " + removedCount + " missions from missioncycle.";
        }

        //Send Rcon-Command to server.
        private static string SendRconCommand(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string param;
            if (!FindRequestValue(request, "paramvalue", out param)) return "ParameterValue not found";

            Rcon.BERcon rcon = new Rcon.BERcon();
            return rcon.SendCommand(param, server.RconParams.IPAddress, server.RconParams.Port, server.RconParams.Password);
        }


        public static string HandleRequest(List<string[]> request)
        {
            string requestName;
            if (!FindRequestValue(request, "request", out requestName)) return "Request was not defined.";

            string sid;
            int serverID = -1;

            if (FindRequestValue(request, "serverid", out sid))
                int.TryParse(sid, out serverID);

            switch (requestName)
            {
                case "updateserverparam":
                    return UpdateServerParam(request, serverID);

                case "updateserverparamstate":
                    return UpdateServerParamState(request, serverID);

                case "updateport":
                    return UpdatePort(request, serverID, "GamePort");

                case "updatequeryport":
                    return UpdatePort(request, serverID, "QueryPort");

                case "updatequeryip":
                    return UpdateIPAddress(request, serverID, "QueryIP");

                case "updaterconip":
                    return UpdateIPAddress(request, serverID, "RconIP");

                case "updaterconport":
                    return UpdatePort(request, serverID, "RconPort");

                case "updaterconpassword":
                    return UpdateRconPassword(request, serverID);

                case "updateprofilename":
                    return UpdateServerProfileName(request, serverID);

                case "startserver":
                    string startResult;
                    Arma3ServerUtility.RestartServer(Arma3ServerUtility.FindServerProcessPairByID(serverID), out startResult);
                    return startResult;

                case "stopserver":
                    string stopResult;
                    Arma3ServerUtility.StopServer(Arma3ServerUtility.FindServerProcessPairByID(serverID), out stopResult);
                    return stopResult;

                case "serverinfo":
                    return GetServerInfo(request, serverID);

                case "queryinfo":
                    return GetQueryInfo(request, serverID);

                case "deletemission":
                    return RemoveMissionFromCycle(request, serverID);

                case "getmissionfiles":
                    return new JavaScriptSerializer().Serialize(GetMissionFiles());

                case "addnewserver":
                    return "Added new server with id " + Arma3ServerUtility.CreateNewServer().ToString();

                case "addmissiontocycle":
                    return AddMissionToCycle(request, serverID);

                case "serverlist":
                    return new JavaScriptSerializer().Serialize(Arma3ServerUtility.ServerList.Select(x => new { x.serverData.ServerID, x.serverData.HostName }).ToArray());

                case "sendrconcommand":
                    return SendRconCommand(request, serverID);

                default:
                    return "That kind of request doesn't exists!";
            }

        }

        //Find requestname from received requestString pair.
        public static bool FindRequestValue(List<string[]> requestArray, string requestName, out string value)
        {
            value = requestArray.Find(x => x[0] == requestName)[1];
            if (value != null) return true;

            return false;
        }

        //Find all mission files in MPMissions folder.
        private static string[] GetMissionFiles()
        {
            List<string> missionList = new List<string>();
            foreach (string file in Directory.GetFiles(appSettings.MissionPath))
            {
                string fName = Path.GetFileName(file);
                if (Path.GetExtension(fName).Equals(".pbo", StringComparison.OrdinalIgnoreCase))
                {
                    missionList.Add(Path.GetFileNameWithoutExtension(fName));
                    Console.WriteLine(fName);
                }
            }

            return missionList.ToArray();
        }
    }
}
