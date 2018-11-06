using System.Linq;
using System;

using Banshee.Packets;

namespace Banshee
{
    public class Protocol
    {
        public static IPacket[] packets = new IPacket[0xff];

        public static void init(){
            initTCP();
            initUDP();
        }
        
        public static byte[] preparePacket(IPacket o){
            byte[] data = o.toBytes();
            int size = data.Length;
            byte[] arr = new byte[size+4];

            arr[0] = 0xf7;
            arr[1] = packetId(o);

            int pktsize = size+4;            
            arr[2] = (byte)(pktsize&0xff);
            arr[3] = (byte)(pktsize>>8);

            Array.Copy(data,0,arr,4,size);
            return arr;
        }

        public static void initTCP(){
            packets[0x01] = new x01PINGFROMHOST(); 
            packets[0x04] = new x04SLOTINFOJOIN(); 
            packets[0x05] = new x05REJECTJOIN(); 
            packets[0x06] = new x06PLAYERINFO(); 
            packets[0x07] = new x07PLAYERLEAVE(); 
            packets[0x08] = new x08OTHERGAMELOADED(); 
            packets[0x09] = new x09SLOTINFO(); 
            packets[0x0A] = new x0aCOUNTDOWNSTART(); 
            packets[0x0B] = new x0bCOUNTDOWNEND(); 
            packets[0x0c] = new x0cACTIONBROADCAST(); 
            packets[0x0F] = new x0fCHATFROMHOST(); 

            packets[0x1E] = new x1eJOINREQUEST(); 
            
            packets[0x21] = new x21PLAYERLEAVEREQ(); 
            packets[0x23] = new x23OWNGAMELOADED(); 
            packets[0x26] = new x26CLIENTACTION(); 
            packets[0x27] = new x27CLIENTKEEPALIVE(); 
            packets[0x28] = new x28CHATTOHOST(); 
            packets[0x3d] = new x3dMAPCHECK(); 

            packets[0x42] = new x42MAPSIZEVERIFY(); 
            packets[0x46] = new x46PONGTOHOST(); 
            packets[0x48] = new x48EXTRAACTIONBROADCAST(); 
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