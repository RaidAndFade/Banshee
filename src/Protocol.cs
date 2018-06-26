using System.Linq;

using WC3_PROTOCOL.packets;

namespace WC3_PROTOCOL
{
    public class Protocol
    {
        public static IPacket[] packets = new IPacket[0xff];

        public static void init(){
            initUDP();
        }

        public static void initUDP(){
            packets[0x2F] = new x2fREQUESTGAME(); 
            packets[0x30] = new x30GAMEDETAILS(); 
            packets[0x31] = new x31CREATEGAME(); 
            packets[0x32] = new x32REFRESHGAME(); 
        }

        public static byte packetId(object o){
            for (byte i = 0; i < 0xff; i++)
            {
                if(packets[i] == null) continue;

                if(o.GetType() == packets[i].GetType()){
                    return i;
                }
            }
            throw new System.Exception("Invalid Packet provided");
        }
    }
}