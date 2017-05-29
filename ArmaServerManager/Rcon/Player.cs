using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaServerManager.Rcon
{
    [Serializable]
    public class Player
    {
        public int ID { get; set; }
        public string PlayerName { get; set; }
        public string IPAddress { get; set; }
        public int Ping { get; set; }
        public string BEGuid { get; set; }

        public Player(int id, string ip, int ping, string guid, string name)
        {
            ID = id;
            Ping = ping;
            IPAddress = ip;
            PlayerName = name;
            BEGuid = guid;
        }
    }
}
