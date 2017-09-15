using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaServerManager.Rcon
{
    [Serializable]
    public class RconDataSet
    {
        public List<Player> PlayerList = new List<Player>();
        public List<string> ServerMessages = new List<string>();
        public List<string> RconLogs = new List<string>();

        public void Clear()
        {
            PlayerList.Clear();
            ServerMessages.Clear();
            RconLogs.Clear();
        }
    }
}
 