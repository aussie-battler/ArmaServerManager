using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArmaServerManager
{

    [Serializable]
    public class QueryParameters
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public bool Enabled { get; set; }
    }
}
 