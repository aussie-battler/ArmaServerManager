using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

namespace ArmaServerManager
{
    public static class SettingsManager
    {
        public static Settings LoadSettings()
        {
            if (!File.Exists("cfg")) return RequestSettings();
            else
            {
                FileStream stream = File.OpenRead("cfg");
                var formatter = new BinaryFormatter();
                Settings s = (Settings)formatter.Deserialize(stream);
                stream.Close();
                return s;
            }
        }

        public static void SaveSettings(Settings settings)
        {
            if (File.Exists("cfg")) File.Delete("cfg");

            FileStream stream = File.Create("cfg");
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, settings);
            stream.Close();
        }

        public static Settings RequestSettings()
        {
            Settings settings = new Settings();

            Console.WriteLine("Existing settings not found.. Creating new:\r\n\r\n");

            Console.WriteLine("Port for this service?");
            settings.ManagerPort = Convert.ToInt32(Console.ReadLine());

            Console.WriteLine("Password for this service?");
            settings.Password = Console.ReadLine();

            Console.WriteLine("Arma3Server executable path?");
            settings.Arma3ServerExePath = Console.ReadLine();

            Console.WriteLine("Path for serverdata?");
            settings.ArmaServersDataPath = Console.ReadLine();

            Console.WriteLine("BattlEye path?");
            settings.BattlEyePath = Console.ReadLine();

            settings.MissionPath = Path.GetDirectoryName(settings.Arma3ServerExePath);

            SaveSettings(settings);

            return settings;
        }
    }
}
