using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ArmaServerManager.A3S
{
    [Serializable]
    public class ServerListStorageObject
    {
        public int ProcessID = -1;
        public string ProcessName = "";
        public Arma3Server ServerData;
        public List<ScheduledEvent> Events;

        public ServerListStorageObject(Process p, Arma3Server s, List<ScheduledEvent>evts)
        {
            if (p != null)
            {
                ProcessID = p.Id;
                ProcessName = p.ProcessName;
            }
            ServerData = s;
            Events = evts;
        }
    }
}
