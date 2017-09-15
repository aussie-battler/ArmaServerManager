using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace ArmaServerManager.A3S
{
    [Serializable]
    class Arma3MissionClass : Arma3ClassObject
    {
        public Arma3MissionClass(string name, string mission, string difficulty)
            : base(name)
        {
            this.ClassMembers.Add(new SrvParam("template", mission, true, true));
            this.ClassMembers.Add(new SrvParam("difficulty", difficulty, true, true));
        }
    }
}
