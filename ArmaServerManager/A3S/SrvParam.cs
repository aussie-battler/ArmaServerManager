using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
 
namespace ArmaServerManager.A3S
{
    [Serializable]
    public class SrvParam
    {
        public string paramName;
        public object paramValue;

        public bool surroundWithQuotation;
        public bool include;

        public SrvParam(string name, object value, bool includeByDefault, bool surr = false)
        {
            paramName = name;
            paramValue = value;
            include = includeByDefault;
            surroundWithQuotation = surr;
        }

        public void SetValue(object value)
        {
            paramValue = value;
        }
    }
}
