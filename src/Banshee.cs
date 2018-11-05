using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Linq;
using System.IO;
using System.IO.Pipes;

using Banshee.packets;

namespace Banshee
{

    public class Banshee
    {

        public const string BOTNAME = "|c00ff0000BNSHE";
        public const string WC3PATH = "Warcraft III";
        public const string MAPPATH = "Maps/FrozenThrone/(2)EchoIsles.w3x";
        public UdpClient udp;
        Thread udpListener;
        Game g;

        static void Main(string[] args)
        {
            //new Map(MAPPATH);
            new Banshee();
        }

        Banshee(){
            GetDependencies();

            Protocol.init();
            udp = new UdpClient(6112);
            udp.EnableBroadcast = true;

            g = new Game(this,MAPPATH);

            udpListener = new Thread(listenUDP);
            udpListener.Start(); 
        }

        void GetDependencies(){
            if(!Directory.Exists("dep"))
                Directory.CreateDirectory("dep");

            string patchMpqPath = WC3PATH+"/War3x.mpq";

            if(!File.Exists(patchMpqPath)){
                patchMpqPath = WC3PATH+"/War3Patch.mpq";
            }

            // using (MpqArchive a = new MpqArchive(patchMpqPath)){
            //     using(MpqStream s = a.OpenFile("Scripts\\common.j")){
            //         using(FileStream f = File.Create("dep/common.j")){
            //             s.CopyTo(f);
            //         }
            //     }
            //     using(MpqStream s = a.OpenFile("Scripts\\blizzard.j")){
            //         using(FileStream f = File.Create("dep/blizzard.j")){
            //             s.CopyTo(f);
            //         }
            //     }
            // }
        }
        void listenUDP(){
            while(true){
                try{
                    IPEndPoint addr = new IPEndPoint(IPAddress.Broadcast, 6112);
                    var data = udp.Receive(ref addr);
                    
                    Console.WriteLine(string.Join('-',data));
                    if(data[0] != 0xF7) continue;
                    
                    byte pckId = data[1];
                    IPacket pckType = Protocol.packets[pckId];
                    int size = (data[2])+(data[3]);

                    if(pckType == null){
                        Console.WriteLine("UNSUPPORTED PACKET (0x"+pckId.ToString("X")+") RECIEVED");
                        continue;
                    }

                    Console.WriteLine("PACKET RECIEVED ("+size+"b): " + pckType);

                    data = data.Skip(4).ToArray();

                    using(BinaryReader br = new BinaryReader(new MemoryStream(data))){
                        onPacket(pckType.parse(br), addr);
                    }
                }catch(Exception e){
                    System.Console.WriteLine(e);
                }
            }
        }


        async void onPacket(Object packet, IPEndPoint from){
            try{
                if(packet is x2fREQUESTGAME){
                    x2fREQUESTGAME p = (x2fREQUESTGAME) packet;
                    if(g.id == p.gameId){
                        Console.WriteLine("Responding to gameinforequest");
                        g.sendGameDetails(from);
                    }
                }else if(packet is x31CREATEGAME){
                    x2fREQUESTGAME p = new x2fREQUESTGAME();
                    p.product = "W3XP";
                    p.version = 30;
                    p.gameId = ((x31CREATEGAME)packet).gameId;
                    sendUDPPacket(p,from);
                }else if(packet is x30GAMEDETAILS){
                    x30GAMEDETAILS p = (x30GAMEDETAILS) packet;
                    Console.WriteLine("GAME CREATED IN LAN : ");
                    Console.WriteLine(p.product);
                    Console.WriteLine(p.gameId);
                    Console.WriteLine(p.gameName);
                    Console.WriteLine(p.players + " / " + p.slots); 
                    Console.WriteLine(string.Join('-',p.stats));
                }else if(packet is x32REFRESHGAME){
                    //idgaf lol
                }else{
                    Console.WriteLine("Unhandled packet " + packet.GetType().Name + " received.");
                }
            }catch(Exception x){
                System.Console.WriteLine(x);
            }
        }

        public void sendUDPPacket(IPacket p, IPEndPoint to){
            sendUDPPacket(Protocol.preparePacket(p),to);
        }
        public void sendUDPPacket(byte[] data, IPEndPoint to){
            udp.Send(data,data.Length,to);
        }
    }
}
