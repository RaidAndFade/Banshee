using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x27CLIENTKEEPALIVE : IPacket
    {
        public int checksum;
        public IPacket parse(BinaryReader br, int len){
            x27CLIENTKEEPALIVE p = new x27CLIENTKEEPALIVE();
            br.ReadByte();
            p.checksum = br.ReadInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.Add(0);
            d.AddRange(BitConverter.GetBytes(checksum));
            return d.ToArray();
        }
    }
}