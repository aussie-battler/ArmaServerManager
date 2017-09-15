using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ArmaServerManager
{
    class SourceQuery
    {
        UdpClient client;
        IPEndPoint ep;

        Dictionary<string, string> serverInfo = new Dictionary<string, string>();
        string serverRules = "";

        //A2S_INFO FUNCTIONS
        public Dictionary<string, string> GetServerInfo(string ip, int port)
        {
            try
            {
                serverInfo.Clear();
                client = new UdpClient();
                ep = new IPEndPoint(IPAddress.Parse(ip), port);
                client.Client.ReceiveTimeout = 250;
                client.Client.SendTimeout = 250;
                client.Connect(ep);
                A2S_INFO_RECEIVE();
                client.Close();
                return serverInfo;
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("Parameter was null: {0}", e.ParamName);
                return new Dictionary<string, string>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return new Dictionary<string, string>();
            }
        }

        private void A2S_INFO_RECEIVE()
        {
            List<byte[]> requestPacket = new List<byte[]>();
            byte[] header = { 0xFF, 0xFF, 0xFF, 0xFF, 0x54 };
            byte[] payload = Encoding.UTF8.GetBytes("Source Engine Query");

            //HEADER STILL EXISTS SO REMOVING IT
            requestPacket.Add(header);
            requestPacket.Add(payload);
            requestPacket.Add(new byte[] { 0 });

            byte[] packet = requestPacket.SelectMany(a => a).ToArray();
            client.Send(packet, packet.Length);

            var receivedData = client.Receive(ref ep);
            A2S_INFO_HANDLE(receivedData);
        }

        private void A2S_INFO_HANDLE(byte[] data)
        {
            byte[] header = new byte[4];
            Buffer.BlockCopy(data, 0, header, 0, header.Length);
            long head = BitConverter.ToInt32(header, 0);
            if (head == -1)//-1 = ei muita paketteja
            {
                byte[] packet = new byte[data.Length - 4];
                Buffer.BlockCopy(data, 4, packet, 0, packet.Length);
                A2S_INFO_PARSE(packet);
            }
            else if (head == -2)//-2 = lisää paketteja tulossa
            {
                try
                {
                    byte[] multipacket = MultiPacketHandle(data, "normal");
                    A2S_INFO_PARSE(multipacket);
                }
                catch (Exception)
                {
                    Console.WriteLine("Error in parsing data");
                }

            }
            else
            {
                Console.WriteLine("Received unknown header");
            }
        }

        private void A2S_INFO_PARSE(byte[] data)
        {
            string[] dicts = new string[] { "servername", "map", "game", "mission" };
            List<byte> message = new List<byte>(data);
            message.RemoveRange(0, 2);//Removing header and protocol bytes

            //Getting string data from packet
            for (int i = 0; i < 4; i++)
            {
                int index = FindNullTerminator(message.ToArray());
                serverInfo.Add(dicts[i], Encoding.UTF8.GetString(message.GetRange(0, index).ToArray()));
                message.RemoveRange(0, index + 1);
            }

            //Getting Application id
            short appid = BitConverter.ToInt16(message.GetRange(0, 2).ToArray(), 0);
            message.RemoveRange(0, 2);
            serverInfo.Add("appid", appid.ToString());

            //Getting current players
            byte players = message[0];
            message.RemoveRange(0, 1);
            serverInfo.Add("players", players.ToString());

            //Getting playerlimit
            byte playerlimit = message[0];
            message.RemoveRange(0, 2);//skipping botval
            serverInfo.Add("maxplayers", playerlimit.ToString());

            //Getting server type
            switch (message[0])
            {
                case 0x64:
                    serverInfo.Add("servertype", "Dedicated");
                    break;
                case 0x6C:
                    serverInfo.Add("servertype", "Non-Dedicated");
                    break;
                default:
                    serverInfo.Add("servertype", "Unknown");
                    break;
            }
            message.RemoveRange(0, 1);

            //Getting server platform
            switch (message[0])
            {
                case 0x77:
                    serverInfo.Add("platform", "Windows");
                    break;
                case 0x6C:
                    serverInfo.Add("platform", "Linux");
                    break;
                case 0x6D | 0x6F:
                    serverInfo.Add("platform", "Mac");
                    break;
                default:
                    serverInfo.Add("platform", "Unknown");
                    break;
            }
            message.RemoveRange(0, 1);

            //Getting visibility (password protection)
            serverInfo.Add("password", message[0].ToString());
            message.RemoveRange(0, 1);

            //Getting VAC-protection state
            serverInfo.Add("vac", message[0].ToString());
            message.RemoveRange(0, 1);

            //Server version
            int nullSpot = FindNullTerminator(message.ToArray());
            serverInfo.Add("version", Encoding.UTF8.GetString(message.GetRange(0, nullSpot).ToArray()));
            message.RemoveRange(0, nullSpot + 1);

            //Getting extra dataflag indicator
            byte edf = message[0];
            message.RemoveRange(0, 1);

            //GamePort found
            if ((edf & 0x80) != 0)
            {
                short port = BitConverter.ToInt16(message.GetRange(0, 2).ToArray(), 0);
                message.RemoveRange(0, 2);
                serverInfo.Add("port", appid.ToString());
            }

            //Steam ID found
            if ((edf & 0x10) != 0)
            {
                ulong id = BitConverter.ToUInt64(message.GetRange(0, 8).ToArray(), 0);
                message.RemoveRange(0, 8);
                serverInfo.Add("steamid", appid.ToString());
            }

            //Useless stuff found.. Ignoring
            if ((edf & 0x40) != 0)
            {
                message.RemoveRange(0, 2);
                nullSpot = FindNullTerminator(message.ToArray());
                message.RemoveRange(0, nullSpot + 1);
            }

            //Game Tags found
            if ((edf & 0x20) != 0)
            {
                nullSpot = FindNullTerminator(message.ToArray());
                serverInfo.Add("flags", Encoding.UTF8.GetString(message.GetRange(0, nullSpot).ToArray()));
                message.RemoveRange(0, nullSpot + 1);
            }

            //More useless stuff found.. Ignoring
            if ((edf & 0x01) != 0)
            {
                ulong id = BitConverter.ToUInt64(message.GetRange(0, 8).ToArray(), 0);
                message.RemoveRange(0, 8);
            }

            //Parsing tags to own dicts
            try
            {
                serverInfo.Add("battleye", GetTagValue(serverInfo["flags"], ",b"));
                serverInfo.Add("serverstate", GetTagValue(serverInfo["flags"], ",s"));
                serverInfo.Add("difficulty", GetTagValue(serverInfo["flags"], ",i"));
                serverInfo.Add("gametype", GetTagValue(serverInfo["flags"], ",t"));
                serverInfo.Add("language", GetTagValue(serverInfo["flags"], ",g"));
                serverInfo.Add("location", GetTagValue(serverInfo["flags"], ",c"));
                serverInfo.Add("requiredversion", GetTagValue(serverInfo["flags"], ",r"));
                serverInfo.Add("verifysignatures", GetTagValue(serverInfo["flags"], ",v"));
            }
            catch (Exception)
            {
                Console.WriteLine("Error parsing data");
            }

        }

        //Getting value for specific tag
        private string GetTagValue(string haystack, string tag)
        {
            int tagpos = haystack.IndexOf(tag) + 2;
            int secondTagPos = haystack.IndexOf(",", tagpos);

            return haystack.Substring(tagpos, secondTagPos - tagpos);
        }


        //A2S_RULES FUNCTIONS

        public string GetServerRules(string ip, int port)
        {
            client = new UdpClient();
            ep = new IPEndPoint(IPAddress.Parse(ip), port);
            client.Client.ReceiveTimeout = 250;
            client.Client.SendTimeout = 250;
            try
            {
                client.Connect(ep);
                A2S_RULES_RECEIVE();
                client.Close();
                return serverRules;
            }
            catch (SocketException e)
            {
                Console.WriteLine(e.Message);
            }
            return string.Empty;
        }


        private void A2S_RULES_RECEIVE()
        {
            List<byte[]> requestPacket = new List<byte[]>();
            byte[] header = { 0xFF, 0xFF, 0xFF, 0xFF, 0x56, 0xFF, 0xFF, 0xFF, 0xFF };

            requestPacket.Add(header);

            byte[] packet = requestPacket.SelectMany(a => a).ToArray();
            client.Send(packet, packet.Length);

            var receivedData = client.Receive(ref ep);
            List<byte> challengeResponse = new List<byte>(receivedData);

            challengeResponse[4] = 0x56;
            client.Send(challengeResponse.ToArray(), challengeResponse.ToArray().Length);
            receivedData = client.Receive(ref ep);

            A2S_RULES_HANDLE(receivedData);
        }

        private void A2S_RULES_HANDLE(byte[] data)
        {
            byte[] header = new byte[4];
            Buffer.BlockCopy(data, 0, header, 0, header.Length);
            long head = BitConverter.ToInt32(header, 0);
            if (head == -1)//-1 = ei muita paketteja
            {
                byte[] packet = new byte[data.Length - 4];
                Buffer.BlockCopy(data, 4, packet, 0, packet.Length);
                A2S_RULES_PARSE(packet);
            }
            else if (head == -2)//-2 = lisää paketteja tulossa
            {
                byte[] multipacket = MultiPacketHandle(data, "normal");
                A2S_RULES_PARSE(multipacket);
            }
            else
            {
                Console.WriteLine("Received unknown header");
            }
        }

        private void A2S_RULES_PARSE(byte[] data)
        {
            //HEADER STILL EXISTS
            var tempData = new List<byte>(data);
            tempData.RemoveRange(0, 3);
            var finalData = tempData.ToArray();
            for (int i = 0; i < finalData.Length; i++)
            {
                if (finalData[i] == 0)
                    finalData[i] = 124;
            }
            Console.WriteLine(Encoding.UTF8.GetString(finalData));
        }





        //COMMON FUNCTIONS USED IN ALL A2S TYPES
        private byte[] MultiPacketHandle(byte[] packet, string a2sType)
        {
            //Length of useless data in beginnin of the packet, so we can remove it to combine all packets.
            int crapLength = 0;
            if (a2sType == "orangebox")
                crapLength = 12;
            else if (a2sType == "normal")
                crapLength = 10;

            int numPackets = packet[8];
            var packetCollection = new Dictionary<int, byte[]>();
            List<byte[]> finalPacket = new List<byte[]>();
            packetCollection.Add(packet[9], packet);

            while (packetCollection.Count < numPackets)
            {
                var morePackets = client.Receive(ref ep);
                packetCollection.Add(morePackets[9], morePackets);
            }

            for (int i = 0; i < numPackets; i++)
            {
                //Poistetaan header (0xFFFFFFFF), joka sisältyy vain ensimmäiseen pakettiin
                int delLength;
                if (i == 0)
                    delLength = crapLength + 4;
                else
                    delLength = crapLength;


                byte[] tempPacket = new byte[packetCollection[i].Length - delLength];
                Buffer.BlockCopy(packetCollection[i], delLength, tempPacket, 0, tempPacket.Length);
                finalPacket.Add(tempPacket);
            }

            return finalPacket.SelectMany(a => a).ToArray();
        }

        private int FindNullTerminator(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                    return i;
            }
            return -1;//Null terminaattoria ei löytynyt
        }
    }
} 
