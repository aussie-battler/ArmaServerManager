using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ArmaServerManager
{
    [Serializable]
    public class Settings
    {
        public int ManagerPort { get; set; }
        public string Arma3ServerExePath { get; set; }
        public string BattlEyePath { get; set; }
        public string ArmaServersDataPath { get; set; }
        public string Password { get; set; }
        public string MissionPath { get; set; }
    }
}
 