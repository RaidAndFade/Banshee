using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x21PLAYERLEAVEREQ : IPacket
    {
        public uint reason;

        public IPacket parse(BinaryReader br, int len){
            x21PLAYERLEAVEREQ p = new x21PLAYERLEAVEREQ();
            p.reason=br.ReadUInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(BitConverter.GetBytes(reason));
            return d.ToArray();
        }
    }
}