using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.IO;
using System.Diagnostics;

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
            SrvProcPair serverProcessPair = Arma3ServerUtility.FindServerProcessPairByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";
            if (serverProcessPair == null) return "Failed to find running server process";

            string param;
            if (!FindRequestValue(request, "paramvalue", out param)) return "ParameterValue not found";

            if (serverProcessPair.RemoteConsole == null) return "Failed to send command. Rcon not initialized";
            if (!serverProcessPair.RemoteConsole.RconEnabled) return "Failed to send command. Rcon is not enabled";

            serverProcessPair.RemoteConsole.Send(param);

            return "Rcon command-request sent";
        }

        //Initialize new Rcon connection to server.
        private static string StartRcon(int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            SrvProcPair serverProcessPair = Arma3ServerUtility.FindServerProcessPairByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";
            if (serverProcessPair == null) return "Failed to find running server process";

            try
            {
                if (serverProcessPair.RemoteConsole == null) serverProcessPair.RemoteConsole = new Rcon.Rcon(server.RconParams.IPAddress, server.RconParams.Port, server.RconParams.Password);
                serverProcessPair.RemoteConsole.SetRconEnabled(true);

                if(serverProcessPair.RemoteConsole.RconEnabled) return "Rcon Started";
                return "Failed to start Rcon";
            }
            catch (Exception e)
            {
                return "Failed to initialize Rcon Conncetion: " + e.Message;
            }
        }

        //Stop Rcon connection.
        private static string StopRcon(int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            SrvProcPair serverProcessPair = Arma3ServerUtility.FindServerProcessPairByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";
            if (serverProcessPair == null) return "Failed to find running server process";

            if (serverProcessPair.RemoteConsole != null)
            {
                serverProcessPair.RemoteConsole.SetRconEnabled(false);
            }

            return "Rcon is stopped";
        }

        //Get Rcon Data
        private static string GetRconData(int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            SrvProcPair serverProcessPair = Arma3ServerUtility.FindServerProcessPairByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";
            if (serverProcessPair == null) return "Failed to find running server process";

            if (serverProcessPair.RemoteConsole == null) return "Failed to get data. Rcon is not enabled";

            return new JavaScriptSerializer().Serialize(serverProcessPair.RemoteConsole.Handler.Data);
        }

        //Update generic settings from this application.
        private static string UpdateGeneralSettings(List<string[]> request)
        {


            string managerport, serverexe, bepath, serverdatapath, password, missionpath;
            if (!FindRequestValue(request, "managerport", out managerport)) return "ManagerPort not found";
            if (!FindRequestValue(request, "bepath", out bepath)) return "BattlEye path not found";
            if (!FindRequestValue(request, "serverexe", out serverexe)) return "Arma3Server exe path not found";
            if (!FindRequestValue(request, "serverdatapath", out serverdatapath)) return "Server Data path not found";
            if (!FindRequestValue(request, "app_password", out password)) return "Password not found";
            if (!FindRequestValue(request, "missionpath", out missionpath)) return "Mission folder path not found";

            int p;
            if (!int.TryParse(managerport, out p)) return "Port is not integer type";

            appSettings.Arma3ServerExePath = serverexe;
            appSettings.ArmaServersDataPath = serverdatapath;
            appSettings.BattlEyePath = bepath;
            appSettings.ManagerPort = p;
            appSettings.MissionPath = missionpath;
            appSettings.Password = password;

            return "Settings updated";
        }

        //Save current settings and restart application.
        private static string RestartWrapper()
        {
            try
            {
                SettingsManager.SaveSettings(appSettings);
                Process.Start("restart.exe", Process.GetCurrentProcess().Id.ToString());
                Environment.Exit(0);
            }
            catch
            {
            }
            
            
            return "";
        }

        //Convert application settings to json.
        private static string GetGeneralSettings()
        {
            return new JavaScriptSerializer().Serialize(appSettings);
        }

        //Get list of scheduled events from the server and return as a JSON.
        private static string GetServerSchedules(int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            return new JavaScriptSerializer().Serialize(server.Schedules.ServerEvents);
        }

        //Add new scheduled event to server.
        private static string AddServerScheduledEvent(List<string[]> request, int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            string description, eventType, scheduleType, date, interval;

            if (!FindRequestValue(request, "description", out description)) return "Event Description not found";
            if (!FindRequestValue(request, "eventtype", out eventType)) return "Event Action not found";
            if (!FindRequestValue(request, "scheduletype", out scheduleType)) return "Event Repeat type not found";
            if (!FindRequestValue(request, "date", out date)) return "Event date not found";
            if (!FindRequestValue(request, "interval", out interval)) return "Event repeat interval not found";

            EventType evtType = String2EventType(eventType);
            ScheduleType evtScType = String2ScheduleType(scheduleType);
            DateTime evtDate = String2DateTime(date);
            int evtInterval = 0;

            int.TryParse(interval, out evtInterval);

            server.Schedules.ServerEvents.Add( new ScheduledEvent(description,"",evtDate, evtType, evtScType, evtInterval));
            return "Added new scheduled event";
        }

        //Toggle schedules on/off
        private static string ToggleScheduleEnabled(int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            server.Schedules.Enabled = !server.Schedules.Enabled;

            return "Server Scheduling set to " + server.Schedules.Enabled.ToString();
        }

        //Get schedules state
        private static string GetSchedulesEnabled(int serverID)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverID);
            if (server == null) return "Server with id " + serverID + " not found";

            return server.Schedules.Enabled.ToString();
        }







        public static string HandleRequest(List<string[]> request)
        {
            string requestName;
            if (!FindRequestValue(request, "request", out requestName)) return "Request was not defined.";

            string sid;
            int serverID = -1;

            if (FindRequestValue(request, "serverid", out sid))
            {
                if(sid.Length > 0)
                    int.TryParse(sid, out serverID);
            }

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

                case "startrcon":
                    return StartRcon(serverID);

                case "stoprcon":
                    return StopRcon(serverID);

                case "getrcondata":
                    return GetRconData(serverID);

                case "updatesettings":
                    return UpdateGeneralSettings(request);

                case "restartwrapper":
                    return RestartWrapper();

                case "getwrappersettings":
                    return GetGeneralSettings();

                case "getschedules":
                    return GetServerSchedules(serverID);

                case "addscheduledevent":
                    return AddServerScheduledEvent(request, serverID);

                case "toggleschedules":
                    return ToggleScheduleEnabled(serverID);

                case "getschedulesstate":
                    return GetSchedulesEnabled(serverID);

                default:
                    return "That kind of request doesn't exists!";
            }

        }

        //Find requestname from received requestString pair.
        public static bool FindRequestValue(List<string[]> requestArray, string requestName, out string value)
        {
            var found = requestArray.Find(x => x[0] == requestName);
            if (found != null)
            {
                value = found[1];
                return true;
            }
            else
            {
                value = null;
                return false;
            }

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


        private static EventType String2EventType(string s)
        {
            foreach (EventType et in Enum.GetValues(typeof(EventType)))
                if (et.ToString() == s)
                    return et;

            return EventType.STOP;
        }

        private static ScheduleType String2ScheduleType(string s)
        {
            foreach (ScheduleType st in Enum.GetValues(typeof(ScheduleType)))
                if (st.ToString() == s)
                    return st;

            return ScheduleType.Once;
        }

        private static DateTime String2DateTime(string s)
        {
            DateTime date;
            DateTime.TryParseExact(s, "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out date);
            return date;
        }
    }
}
