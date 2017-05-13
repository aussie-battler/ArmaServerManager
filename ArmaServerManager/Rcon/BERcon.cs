using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ArmaServerManager.Rcon
{
    public class BERcon
    {
        private UdpClient client;
        private IPEndPoint ep;
        private const byte AUTH = 0x00, COMMAND = 0x01;

        public string SendCommand(string command, string serverip, int port, string rconpassword)
        {
            try
            {
                client = new UdpClient();
                client.Client.ReceiveTimeout = 3000;
                ep = new IPEndPoint(IPAddress.Parse(serverip), port);
                client.Connect(ep);

                if (Authenticate(rconpassword))
                {
                    return SendCommand(command);
                }

                return "RCON_AUTHENTICATION_FAILED";
            }
            catch (ArgumentNullException e)
            {
                return "Rcon failed: Argument was not defined: " + e.ParamName;
            }
            catch (Exception e)
            {
                return "RCON_ERROR: " + e.Message;
            }


            
        }

        private string SendCommand(string command)
        {
            var response = SendPacket(command, COMMAND);

            Console.WriteLine("Received HexArray:");
            int i = 1;
            foreach (var item in response)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.Write("{0:X}", item);
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write(", {0}", Encoding.ASCII.GetString(new byte[]{item}));
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("]");

                if (i % 8 == 0)
                    Console.WriteLine();
                i++;
            }
            Console.WriteLine();

            if (response.Length > 7)
            {
                GetPlayerData(response);
                return Encoding.ASCII.GetString(response, 9, response.Length - 9);
            }
            return "UNKOWN_RCON_ERROR";
        }

        private bool Authenticate(string password)
        {

            var response = SendPacket(password, AUTH);

            if (response.Length < 9) return false;
            if (response[7] == 0x00 && response[8] == 0x01) return true;

            return false;
        }

        private byte[] SendPacket(string dataString, byte packetType)
        {
            List<byte> data = new List<byte>();
            data.Add(0xFF);
            data.Add(packetType);
            if (packetType != AUTH) data.Add(0x00);
            data.AddRange(Encoding.ASCII.GetBytes(dataString));
            var packet = new List<byte>(GetHeader(data.ToArray())).Concat(data).ToArray();

            client.Send(packet, packet.Length);

            return client.Receive(ref ep);
        }

        private byte[] GetHeader(byte[]subsequentBytes)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(new byte[]{0x42,0x45});
            bytes.AddRange(BitConverter.GetBytes((int)Crc32.Compute(subsequentBytes)));
            return bytes.ToArray();
        }





        public static Player[] GetPlayerData(byte[] data)
        {
            List<Player> playerList = new List<Player>();
            try
            {
                //Remove useless characters from playersdata.
                string stringData = Encoding.ASCII.GetString(data);
                stringData = stringData.Substring(115);
                stringData = stringData.Substring(0, stringData.LastIndexOf(Convert.ToChar(0xA)));

                //Players are separated with 0x0A (line-feed)
                var players = stringData.Split(Convert.ToChar(0xA));

                foreach (var player in players)
                {
                    try
                    {
                        var dataParts = player.Split(new char[] { Convert.ToChar(0x20) }, StringSplitOptions.RemoveEmptyEntries);

                        //Combine last parts because player might have space in name.
                        for (int i = 5; i < dataParts.Length; i++) dataParts[4] += " " + dataParts[i];
                        playerList.Add(new Player(Convert.ToInt32(dataParts[0]), dataParts[1], Convert.ToInt32(dataParts[2]), dataParts[3], dataParts[4]));
                    }
                    catch{}
                }
                return playerList.ToArray();
            }
            catch
            {
                return playerList.ToArray();
            }
        }
    }
}
