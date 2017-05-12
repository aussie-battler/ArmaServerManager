using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaServerManager
{
    [Serializable]
    public class RconParameters
    {
        public string IPAddress;
        public int Port;
        public string Password;
    }
}
