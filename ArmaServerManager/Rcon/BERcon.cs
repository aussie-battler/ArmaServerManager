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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "RCON_FAILED";
            }


            
        }

        private string SendCommand(string command)
        {
            var response = SendPacket(command, COMMAND);
            if (response.Length > 0)
            {
                return Encoding.ASCII.GetString(response);
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
    }
}
