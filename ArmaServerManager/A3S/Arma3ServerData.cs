using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Web.Script.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace ArmaServerManager.A3S
{
    public class Arma3ServerData
    {

        //Get JSON string from Arma3Server class.
        public static bool ServerToJSON(Arma3Server server, out string result)
        {
            if (server != null)
            {
                result = new JavaScriptSerializer().Serialize(server);
                return true;
            }

            result = "Server not defined";
            return false;
        }

        //Update Arma3Server member / property.
        public static string UpdateConfigParam(int serverId, string paramName, object value)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverId);
            if (server == null)
                return "Failed to update config parameter. Given ID didn't match any server.";
            SrvParam param = FindSrvParam(server, paramName);
            if (param == null)
                return "Failed to update config parameter. Given parameter not found";

            param.paramValue = value;
            //Serverlist saving

            return paramName + " updated";

        }

        //Update Arma3Server member/property -state.
        public static string UpdateConfigParamState(int serverId, string paramName, bool state)
        {
            Arma3Server server = Arma3ServerUtility.FindArma3ServerByID(serverId);
            if (server == null)
                return "Failed to update config parameter. Given ID didn't match any server.";
            SrvParam param = FindSrvParam(server, paramName);
            if (param == null)
                return "Failed to update config parameter. Given parameter not found";


            param.include = state;
            //Serverlist saving

            return paramName + " state updated to " + state.ToString();
        }


        //Find SrvParam member by Name from Arma3Server.
        public static SrvParam FindSrvParam(Arma3Server server, string paramName)
        {
            if (server == null) return null;
            foreach (var item in typeof(Arma3Server).GetFields())
            {
                if (item.Name == paramName && item.GetValue(server) is SrvParam)
                    return (SrvParam)item.GetValue(server);
            }

            return null;
        }
    }
}
