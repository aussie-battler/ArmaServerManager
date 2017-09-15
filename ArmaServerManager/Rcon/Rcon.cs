using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace ArmaServerManager.Rcon
{
    public class Rcon 
    {
        public bool RconEnabled { get; private set; }
        public IPAddress IpAddress { get; private set; }
        public int Port { get; private set; }
        public string RconPassword { get; private set; }

        public RconDataHandler Handler = new RconDataHandler();

        private IPEndPoint RemoteEndpoint;
        private UdpClient Client { get; set; }
        private byte sequenceNumber = 0;

        private int errorCount = 0;


        public Rcon(string IpAddress, int port, string password)
        {
            RconEnabled = false;

            IPAddress tempIp;
            if (!IPAddress.TryParse(IpAddress, out tempIp)) throw new ArgumentException("Invalid IP-Address");
            this.IpAddress = tempIp;

            if (port < 0 || port > 65535) throw new ArgumentException("Invalid Port Value");
            this.Port = port;

            this.RconPassword = password;

            try
            {
                this.RemoteEndpoint = new IPEndPoint(this.IpAddress, this.Port);
                this.Client = new UdpClient();
                this.Client.Connect(RemoteEndpoint);
            }
            catch
            {
                throw new Exception("Failed to create UDP-Client");
            }
        }

        public void SetRconEnabled(bool enabled)
        {
            if (!this.RconEnabled)
            {
                if (enabled == true)
                {
                    if (Authenticate(RconPassword))
                    {
                        this.RconEnabled = enabled;
                        System.Threading.Thread t = new System.Threading.Thread(Listen);
                        t.Start();
                    }
                    else
                    {
                        this.RconEnabled = false;
                    }
                }
                else
                {
                    this.RconEnabled = false;
                    sequenceNumber = 0;
                }
            }
        }

        public void Send(string command)
        {
            byte seqNum = SendPacket(command, PacketType.Command_Packet);
            Handler.RequestList.Add(new Tuple<byte, string, DateTime>(seqNum, command, DateTime.Now));
        }

        public void Listen()
        {
            Timer keepAliveTimer = new Timer();
            keepAliveTimer.AutoReset = true;
            keepAliveTimer.Elapsed += keepAliveTimer_Elapsed;
            keepAliveTimer.Interval = 30000;
            keepAliveTimer.Start();

            ReceiveUdp();

            while (RconEnabled)
                System.Threading.Thread.Sleep(200);

            keepAliveTimer.Dispose();
        }

        //BRcon requiers empty command packet to be sent at least every 45 seconds.
        private void keepAliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SendPacket("", PacketType.Command_Packet);
        }

        private void ReceiveUdp()
        {
            Client.BeginReceive(new AsyncCallback(UdpReceiveCallback), null);
        }

        private void UdpReceiveCallback(IAsyncResult ar)
        {
            byte[] received = null;
            try
            {
                received = Client.EndReceive(ar, ref RemoteEndpoint);
                errorCount = 0;
            }
            catch 
            {
                if (errorCount > 5)
                    SetRconEnabled(false);
                errorCount++;

                return;
            }

            RconPacket p = ParseData(received);
            if (p.isValid)
            {
                p.Debug();
                if (p.isMultipacket)
                {
                    Handler.PushMultiPacket(p);
                }
                else if (p.type == PacketType.ServerMessage_Packet)
                {
                    Handler.PushNormalPacket(p);
                    SendPacket("", PacketType.ServerMessage_Packet, p.sequenceNumber);
                }
                else if (p.type == PacketType.Command_Packet)
                {
                    Handler.PushNormalPacket(p);
                }
            }

            if(RconEnabled) ReceiveUdp();
        }

        public static RconPacket ParseData(byte[] data)
        {
            RconPacket rconPacket = new RconPacket();
            rconPacket.receivedDate = DateTime.Now;
            try
            {
                List<byte> datalist = new List<byte>(data);

                datalist.RemoveRange(0, 2); //Remove BE-header form packet.

                int crc = BitConverter.ToInt32(datalist.Take(4).ToArray(), 0);
                datalist.RemoveRange(0, 4); //Remove crc from packet.

                int correctCrc = (int)Crc32.Compute(datalist.ToArray());
                datalist.RemoveAt(0); //Remove last byte of header(0xff).

                rconPacket.type = (PacketType)datalist[0];
                datalist.RemoveAt(0); //Remove packet type.

                if (rconPacket.type == PacketType.Login_Packet) //If packet is type of login, then there should be only 1 byte (login state) left.
                {
                    rconPacket.data = datalist.ToArray();
                    rconPacket.isValid = (crc == correctCrc);
                }

                else
                {
                    rconPacket.sequenceNumber = datalist[0];
                    datalist.RemoveAt(0); //Remove sequence number.

                    if (datalist[0] == 0x00) //is multipacket.
                    {
                        datalist.RemoveAt(0); //Remove multipacket header.
                        rconPacket.isMultipacket = true;
                        rconPacket.multiPacketCount = datalist[0];
                        datalist.RemoveAt(0); //Remove multipacket count.
                        rconPacket.multiPacketIndex = datalist[0];
                        datalist.RemoveAt(0); //Remove multipacket index.
                        rconPacket.data = datalist.ToArray();
                        rconPacket.isValid = (crc == correctCrc);
                    }

                    else
                    {
                        rconPacket.isValid = (crc == correctCrc);
                        rconPacket.data = datalist.ToArray();
                    }
                }
            }
            catch
            {
                rconPacket.isValid = false;
            }

            return rconPacket;
        }

        private bool Authenticate(string password)
        {

            SendPacket(password, PacketType.Login_Packet);
            var response = Client.Receive(ref RemoteEndpoint);
            RconPacket p = Rcon.ParseData(response);
            if (p.type == PacketType.Login_Packet && p.isValid && p.data[0] == 1) return true;
            return false;

        }


        private byte SendPacket(string dataString, PacketType type, byte receivedSequenceNumber = 0)
        {
            List<byte> data = new List<byte>();
            data.Add(0xFF);
            data.Add((byte)type);

            if (type == PacketType.Command_Packet) data.Add(sequenceNumber);
            if (type == PacketType.ServerMessage_Packet) data.Add(receivedSequenceNumber);

            data.AddRange(Encoding.ASCII.GetBytes(dataString));

            var packet = new List<byte>(GetHeader(data.ToArray())).Concat(data).ToArray();

            Client.Send(packet, packet.Length);

            byte oldSeqNum = sequenceNumber;
            if(type == PacketType.Command_Packet) sequenceNumber = (byte) ((sequenceNumber == 255) ? 0 : sequenceNumber + 1);

            return oldSeqNum;
        }

        private byte[] GetHeader(byte[] subsequentBytes)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(new byte[] { 0x42, 0x45 });
            bytes.AddRange(BitConverter.GetBytes((int)Crc32.Compute(subsequentBytes)));
            return bytes.ToArray();
        }


    }


    public class RconPacket
    {
        public PacketType type;
        public byte sequenceNumber;
        public byte[] data;
        public bool isValid = false;

        public bool isMultipacket = false;
        public byte multiPacketCount;
        public byte multiPacketIndex;

        public DateTime receivedDate;

        public override string ToString()
        {
            if(data != null) return Encoding.ASCII.GetString(data);
            return string.Empty;
        }

        public void Debug()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PacketType {0}, sequence-number {1}, validity {2}, IsMultipacket {3}, Multipacket-index {4} / {5}, datastring: {6}", type.ToString(), sequenceNumber, isValid.ToString(), isMultipacket.ToString(), multiPacketIndex, multiPacketCount -1,  ToString());
            Console.ForegroundColor = ConsoleColor.White;
        }
    }

    public enum PacketType : byte
    {
        Login_Packet = 0,
        Command_Packet = 1,
        ServerMessage_Packet = 2
    };
}
