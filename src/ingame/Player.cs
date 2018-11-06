using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Generic;

using Banshee.Packets;

namespace Banshee.Ingame
{
    public class PotentialPlayer{
        public TcpClient socket;
        public NetworkStream networkStream;
        public Game game;
        public x1eJOINREQUEST joinReq;
        public bool shouldDelete = false, hasError = false;
        public bool isConnected = false;

        public ConnectedPlayer player;
        public PotentialPlayer(Game _g, TcpClient _s, NetworkStream _ns, x1eJOINREQUEST jr){
            game = _g;
            socket = _s;
            networkStream = _ns;
            joinReq = jr;
        }

        public void Close(){
            networkStream.Flush();
            try{
                socket.Client.Disconnect(true);
                socket.Client.Close(0);
                socket.Close();
                socket.Dispose();
            }catch(Exception e){
                Console.WriteLine(e);
            } //idek what can be thrown here but i dont want it :)
        }

        public void rejectJoin(int reason){
            x05REJECTJOIN rj = new x05REJECTJOIN();
            rj.reason = reason;
            sendPacket(rj);
        }

        public void sendPacket(IPacket p){
            byte[] b = Protocol.preparePacket(p);
            networkStream.Write(b,0,b.Length);
        }
    }

    public class ConnectedPlayer{
        public PotentialPlayer pp;
        public byte pid;
        public string name;
        public bool loaded = false;
        public List<double> pings = new List<double>();

        public List<byte> netwritebuff = new List<byte>();

        //int lastmappartsent, lastmappartacked, starteddownloadingticks, finishedloadingticks, startedlaggingticks
        //bool muted, wantstodrop, wantstokick, islagging, hasleft 

        public List<IPacket> packets = new List<IPacket>();

        public Thread networkThread;

        public int syncCounter;
        public List<int> checksumArr = new List<int>();
        public int latestChecksum;

        public ConnectedPlayer(PotentialPlayer _pp, byte _pid, string _name){
            pp = _pp;
            pid = _pid;
            name = _name;
            networkThread = new Thread(handleReceived);
            networkThread.Start();
        }

        public void Close(){
            pp.Close();
            pp.shouldDelete = true;
            Console.WriteLine("[*] Client "+pid+" has been removed.");
        }

        public void tick(){
            if(pp.shouldDelete || pp.hasError) return;

            if(pp.isConnected && !isConnected()){
                Console.WriteLine("Client "+pid+" is no longer properly connected. Dropping due to error.");
                pp.hasError = true;
            }

            IPacket[] packetarr = packets.ToArray();
            packets.Clear();
            foreach (var p in packetarr)
            {
                onPacket(p);
            }
        }


        public void handleReceived(){
            try
            {
                while(pp.game.running && !pp.shouldDelete && !pp.hasError){
                    if(pp.networkStream.ReadByte()==0xf7){
                        byte pktid = (byte)pp.networkStream.ReadByte();
                        ushort len = (ushort)(pp.networkStream.ReadByte() + (pp.networkStream.ReadByte()<<8));
                        byte[] data = new byte[len];
                        pp.networkStream.Read(data,0,len-4);
                        IPacket pckType = Protocol.packets[pktid];
                        if(pckType == null){
                            Console.WriteLine("UNSUPPORTED PACKET (0x"+pktid.ToString("X")+") RECIEVED");
                            continue;
                        }
                        //Console.WriteLine("TCP PACKET RECIEVED ("+len+"b): " + pckType);
                        if(pckType is x46PONGTOHOST){
                            using(BinaryReader br = new BinaryReader(new MemoryStream(data))){
                                onPingPacket(pckType.parse(br, len-4));
                            }
                            continue;
                        }

                        using(BinaryReader br = new BinaryReader(new MemoryStream(data))){
                            packets.Add(pckType.parse(br, len-4));
                        }
                    }else{
                        Console.WriteLine("Recieved malformed header, Dropping client");
                        pp.hasError = true;
                        break;
                    }
                }
                pp.hasError = true;
                Console.WriteLine("Not reading packets anymore, this client is dead");
            }catch (SocketException){
                Console.WriteLine("Client "+pid+" disconnected.");
                pp.hasError = true;
            }
        }

        void onPingPacket(IPacket packet){
            var p = (x46PONGTOHOST)packet;

            //Basically, since i dont actually need to get the values of the two times (just the diff), and since the max possible value for this
            // before the client disconnects itself is around 30 seconds, giving myself an hour to calculate difference should be more than enough.
            //This raises a problem when p.nonce>ms%1mil, because then it turns negative (and large), leading to a not-so-accurate ping
            var msping = pp.game.GetTimeMS()%1000000 - p.nonce; 

            //this theoretically fixes the possible problem of nonce>ms%1mil. because the difference between p.nonce and 0 + the diff between 
            // ms%1mil and 1mil should add up to equal the same diff as if it was normal.

            //i may be dumb, but this can probably also be msping = 1000000+msping. I'm going to play it safe and recalculate all using positives though.
            if(msping<0) msping = (1000000-pp.game.GetTimeMS()%1000000)+(p.nonce); 

            //The benefits of this: allows for sub-tps accuracy without having to use ticks or something weird like that
            //The costs of this, (partly explained on line 116, it has possibility for failure that i have probably not forseen)

            // Console.WriteLine("Ping of "+pid+" is "+msping+"ms");

            pings.Add(msping);
            while(pings.Count>10){ //shit expensive but should only happen once every ping for one iteration so probably fine lol
                pings.RemoveAt(0);
            }
        }
        
        async void onPacket(IPacket packet){
            if(packet is x42MAPSIZEVERIFY){
                var p = (x42MAPSIZEVERIFY)packet;
                Console.WriteLine(string.Join("-",p.mapSize));
                Console.WriteLine(string.Join("-",pp.game.gameMap.MapSize));
                if(p.mapSize.SequenceEqual(pp.game.gameMap.MapSize)){
                    byte sid = pp.game.GetSlotId(this);
                    if(sid == 255) return;
                    pp.game.slots[sid].downloadStatus = 100;
                    pp.game.UpdateSlots();
                }else{
                    Console.WriteLine("Kicked client, cannot provide maps through download");
                    pp.socket.Close();
                }
            }else if(packet is x28CHATTOHOST){
                var p = (x28CHATTOHOST)packet;
                pp.game.HandleClientChat(this,p);
            }else if(packet is x46PONGTOHOST){
            }else if(packet is x23OWNGAMELOADED){
                loaded=true;
                pp.game.HandleClientGameLoaded(this);
            }else if(packet is x21PLAYERLEAVEREQ){
                var p = (x21PLAYERLEAVEREQ)packet;
                pp.game.HandleClientLeaveRequest(this,p.reason);
            }else if(packet is x26CLIENTACTION){
                pp.game.HandleClientAction(this,(x26CLIENTACTION)packet);
            }else if(packet is x27CLIENTKEEPALIVE){
                var p = (x27CLIENTKEEPALIVE)packet;
                syncCounter++;
                checksumArr.Add(p.checksum);
                pp.game.HandleClientKeepalive(this);
            }
        }

        public double GetPing(){
            if(pings.Count==0)
                return -1;
            return pings.Average();
        }

        public bool isConnected(){
            return true;//!(pp.socket.Client.Poll(0, SelectMode.SelectRead) && pp.socket.Available == 0);
        }

        public void queuePacket(IPacket p){
            byte[] b = Protocol.preparePacket(p);
            netwritebuff.AddRange(b);
        }

        public void flushSocket(){
            byte[] buf = netwritebuff.ToArray();
            netwritebuff.Clear();
            pp.networkStream.Write(buf,0,buf.Length);
        }
    }
    public class Player
    {
        string name;
        byte color;
        byte team;
        byte race;
        byte comptype;
        byte downloadstatus;
        byte slotstatus;
    }
}