using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ArmaServerManager.A3S
{
    [Serializable]
    public class SrvProcPair
    {
        public Process proc;
        public Arma3Server serverData;
    };
}
