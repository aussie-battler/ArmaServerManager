using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ArmaServerManager.Rcon
{
    public class RconDataHandler
    {
        private List<RconPacket> multiPackets = new List<RconPacket>();
        private Timer PacketTimer;

        public List<Tuple<byte, string, DateTime>> RequestList = new List<Tuple<byte, string, DateTime>>();

        public List<RconPacket> ReadyPackets = new List<RconPacket>();
        public RconDataSet Data = new RconDataSet();

        public RconDataHandler()
        {
            PacketTimer = new Timer();
            PacketTimer.AutoReset = true;
            PacketTimer.Interval = 500;
            PacketTimer.Elapsed += PacketTimer_Elapsed;
            PacketTimer.Start();
        }

        private void PacketTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            HandleMultiPackets();
            HandleNormalPackets();
        }

        private void HandleNormalPackets()
        {
            for (int i = RequestList.Count - 1; i >= 0; i--)
            {
                var p = ReadyPackets.Find(x => x.sequenceNumber == RequestList[i].Item1 && x.type != PacketType.ServerMessage_Packet);
                if (p != null)
                {
                    p.Debug();
                    switch (RequestList[i].Item2)
                    {
                        case "players":
                            Data.PlayerList = new List<Player>(GetPlayerData(p.data));
                            //Data.RconLogs.Add(p.ToString());
                            break;
                        default:
                            if (p.type == PacketType.ServerMessage_Packet) Data.ServerMessages.Add(p.ToString());
                            else Data.RconLogs.Add(p.ToString());
                            break;
                    }
                    RequestList.RemoveAt(i);
                }
                else if ((new TimeSpan(DateTime.Now.Ticks).Seconds) - (new TimeSpan(RequestList[i].Item3.Ticks).Seconds) > 5) RequestList.RemoveAt(i);
            }

            //Delete all expired non-server originated packets.
            ReadyPackets.RemoveAll(x => (new TimeSpan(DateTime.Now.Ticks).Seconds) - (new TimeSpan(x.receivedDate.Ticks).Seconds) > 5 && x.type != PacketType.ServerMessage_Packet);

            //Handle server originated messages and remove only those since rcon is running on spearate thread and there might be
            //new packets with no intention to remove.
            for (int i = ReadyPackets.Count -1; i >= 0; i--)
            {
                if (ReadyPackets[i].type == PacketType.ServerMessage_Packet)
                {
                    Data.ServerMessages.Add(ReadyPackets[i].ToString());
                    ReadyPackets.RemoveAt(i);
                }
            }
        }

        private void HandleMultiPackets()
        {
            List<byte> packetIDs = new List<byte>();

            for (int i = multiPackets.Count - 1; i >= 0; i--)
            {
                if ((new TimeSpan(DateTime.Now.Ticks).Seconds) - (new TimeSpan(multiPackets[i].receivedDate.Ticks).Seconds) > 5)
                {
                    multiPackets.RemoveAt(i);
                }
                else if (packetIDs.IndexOf(multiPackets[i].sequenceNumber) == -1) packetIDs.Add(multiPackets[i].sequenceNumber);
            }

            foreach (var item in packetIDs)
            {
                if (multiPackets.Count(x => x.sequenceNumber == item) == multiPackets.Find(x => x.sequenceNumber == item).multiPacketCount)
                {
                    byte[] data = multiPackets.FindAll(x => x.sequenceNumber == item).OrderBy(y => y.multiPacketIndex).Select(z => z.data).SelectMany(a => a).ToArray();
                    PushNormalPacket(new RconPacket() { receivedDate = DateTime.Now, sequenceNumber = item, data = data });
                }
            }
        }

        public void PushMultiPacket(RconPacket packet)
        {
            multiPackets.Add(packet);
        }

        public void PushNormalPacket(RconPacket packet)
        {
            ReadyPackets.Add(packet);
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
                    catch { }
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
