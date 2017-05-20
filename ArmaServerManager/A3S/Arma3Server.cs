using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ArmaServerManager.A3S
{
    [Serializable]
    public class Arma3Server
    {

        public int ServerID;
        public int GamePort;
        public string ServerProfileName = "arma3server";
        public string ExtraConfigLines = "";

        public QueryParameters QueryParams = new QueryParameters();
        public RconParameters RconParams = new RconParameters();


        [NonSerialized]
        public ServerSchedule Schedules = new ServerSchedule();


        #region ServerOptions
        public SrvParam AdminPassword = new SrvParam("passwordAdmin", "", false, true);
        public SrvParam ServerPassword = new SrvParam("password", "", false, true);
        public SrvParam CommandPassword = new SrvParam("serverCommandPassword", "", false, true);
        public SrvParam HostName = new SrvParam("hostname", "ServerName", true, true);
        public SrvParam IntervalMOTD = new SrvParam("motdInterval", 5, false);
        public SrvParam MaxPlayers = new SrvParam("maxPlayers", 10, true);
        #endregion

        #region ServerBehavior
        public SrvParam VoteThreshold = new SrvParam("voteThreshold", 0.5, false);
        public SrvParam VoteMissionPlayers = new SrvParam("voteMissionPlayers", 5, false);
        public SrvParam KickDuplicate = new SrvParam("kickduplicate", 1, false);
        public SrvParam Loopback = new SrvParam("loopback", true, false);
        public SrvParam Upnp = new SrvParam("upnp", 1, false);
        public SrvParam AllowFilePatching = new SrvParam("allowedFilePatching", 0, true);
        public SrvParam DisconnectTimeout = new SrvParam("disconnectTimeout", 90, false);
        public SrvParam MaxDesync = new SrvParam("maxdesync", 150, false);
        public SrvParam MaxPing = new SrvParam("maxping", 200, false);
        public SrvParam MaxPacketLoss = new SrvParam("maxpacketloss", 50, false);
        #endregion

        #region ServerOtherParams
        public SrvParam VerifySignatures = new SrvParam("verifySignatures", 2, true);
        public SrvParam DrawingInMap = new SrvParam("drawingInMap", 0, false);
        public SrvParam DisableVoN = new SrvParam("disableVoN", 0, false);
        public SrvParam VoNCodecQuality = new SrvParam("vonCodecQuality", 3, false);
        public SrvParam VonCodec = new SrvParam("vonCodec", 0, false);
        public SrvParam LogFile = new SrvParam("logFile", "server_console.log", false, true);
        public SrvParam BattlEye = new SrvParam("BattlEye", 1, true);
        public SrvParam TimeStampFormat = new SrvParam("timeStampFormat", "short", false, true);
        public SrvParam ForceRotorLibSim = new SrvParam("forceRotorLibSimulation", 0, false);
        public SrvParam Persistent = new SrvParam("persistent", 0, false);
        public SrvParam RequiredBuild = new SrvParam("requiredBuild", 12345, false);
        public SrvParam ForcedDifficulty = new SrvParam("forcedDifficulty", "regular", false, true);

        #endregion

        #region ServerArrayParams

        public SrvParam ServerMOTD = new SrvParam("motd[]", "{\"Welcome to the server\"}", true, false);
        public SrvParam KickClientsOnSlowNetwork = new SrvParam("kickClientsOnSlowNetwork[]", "{ 0, 0, 0, 0 }", false, false);
        public SrvParam MissionWhiteList = new SrvParam("missionWhitelist[]", "{}", false, false);
        public SrvParam Admins = new SrvParam("admins[]", "{\"1234\"}", false, false);
        public SrvParam HeadlessClients = new SrvParam("headlessClients[]", "{\"127.0.0.1\"}", false, false);
        public SrvParam LocalClient = new SrvParam("localClient[]", "{\"127.0.0.1\"}", false, false);


        #endregion

        #region ServerClassConfigs

        public Arma3ClassObject Missions = new Arma3ClassObject("Missions");

        #endregion


        public override string ToString()
        {
            return HostName.paramValue.ToString();
        }
        public void SetArrayParameter(string[] lines, SrvParam param)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (var item in lines)
            {
                sb.Append("\"").Append(item).Append("\"").Append(",");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("}");
            param.paramValue = sb.ToString();
        }
        public void SetArrayParameter(int[] lines, SrvParam param)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            foreach (var item in lines)
            {
                sb.Append(item).Append(","); 
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append("}");
            param.paramValue = sb.ToString();
        }
        public void InsertSubClass(Arma3ClassObject targetClass, Arma3ClassObject sourceClass)
        {
            targetClass.SubClasses.Add(sourceClass);
        }
    }
}
