using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Globalization;

namespace ArmaServerManager
{
    public static class Arma3ServerConfigWriter
    {
        public static void WriteConfigFile(Arma3Server server, Settings settings)
        {
            CultureInfo oldCulture = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            List<string> lines = new List<string>();

            #region PopulateList
            foreach (var field in typeof(Arma3Server).GetFields())
            {
                var p = field.GetValue(server);

                if (p is SrvParam)
                {

                    SrvParam param = (SrvParam)p;

                    if (param.include)
                    {
                        if (param.surroundWithQuotation)
                            lines.Add(param.paramName + " = \"" + param.paramValue + "\";");
                        else
                            lines.Add(param.paramName + " = " + param.paramValue.ToString() + ";");

                    }
                }

                else if (p is Arma3ClassObject)
                {
                    StringBuilder sb = new StringBuilder();
                    AppendSubClasses((Arma3ClassObject)p, sb, 0);
                    lines.Add(sb.ToString());
                }
            }
            //Same for property fields
            foreach (var field in typeof(Arma3Server).GetProperties())
            {
                var p = field.GetValue(server);
                if (!(p is SrvParam)) continue;

                SrvParam param = (SrvParam)p;

                if (param.include)
                {
                    if (param.surroundWithQuotation)
                        lines.Add(param.paramName + " = \"" + param.paramValue + "\";");
                    else
                        lines.Add(param.paramName + " = " + param.paramValue.ToString() + ";");

                }

            } 



	        #endregion


            StringBuilder configData = new StringBuilder();
            configData.Append("// Automatically generated config file - ").AppendLine(DateTime.Now.ToString()).AppendLine("// With ArmaServerManager\r\n");

            foreach (var item in lines)
            {
                configData.AppendLine(item);
            }


            string path = settings.ArmaServersDataPath + "/" + server.ServerID.ToString();
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            File.WriteAllText(path + "/serverconfig.cfg", configData.ToString());


            Thread.CurrentThread.CurrentCulture = oldCulture;
        }

        public static void WriteProfilesFile(Arma3Server server, Settings settings)
        {
        }

        public static void AppendSubClasses(Arma3ClassObject o, StringBuilder s, int tabs)
        {
            string tab = new String('\t', tabs );
            s.AppendLine().Append(tab).Append("class ").AppendLine(o.ClassName).Append(tab).AppendLine("{");

            foreach (var item in o.ClassMembers)
            {
                if (item.include)
                {
                    if (item.surroundWithQuotation)
                        s.AppendLine().Append(tab + '\t').Append(item.paramName).Append(" = ").Append("\"").Append(item.paramValue).Append("\"").Append(";");
                    else
                        s.AppendLine().Append(tab + '\t').Append(item.paramName).Append(" = ").Append(item.paramValue).Append(";");
                }
            }

            foreach (var item in o.SubClasses)
            {
                AppendSubClasses(item, s, tabs+1);
            }

            s.AppendLine().Append(tab).AppendLine("};");
        }
    }
}
