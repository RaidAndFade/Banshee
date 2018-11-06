using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x06PLAYERINFO : IPacket
    {
        public byte pid;
        public string name;
        public byte[] internalIp, externalIp;
        public IPacket parse(BinaryReader br, int len){
            x06PLAYERINFO p = new x06PLAYERINFO();
            br.ReadInt32(); //unk1
            p.pid = br.ReadByte();
            p.name = ConvertUtils.parseStringZ(br);
            br.ReadByte();
            br.ReadByte();
            br.ReadInt32(); //ext afinet and port
            externalIp = br.ReadBytes(4); //ipv4 ip 
            br.ReadBytes(8); //ipv6? ip (combines with ipv4 to make 12 bytes)
            br.ReadInt32(); //int afinet and port
            internalIp = br.ReadBytes(4); //ipv4 ip 
            br.ReadBytes(8); //ipv6? ip (maybe combines with ipv4 to make 12 bytes)
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(new byte[]{2,0,0,0});
            d.Add(pid);
            d.AddRange(ConvertUtils.fromStringZ(name));
            d.AddRange(new byte[]{1, 0, 2,0,0,0}); // ?,?, af_inet(2,0), port(0,0)
            d.AddRange(externalIp);
            d.AddRange(new byte[]{0,0,0,0, 0,0,0,0});
            d.AddRange(new byte[]{2,0,0,0}); //af_inet (2,0), port (0,0)
            d.AddRange(internalIp);
            d.AddRange(new byte[]{0,0,0,0, 0,0,0,0});
            return d.ToArray();
        }
    }
}