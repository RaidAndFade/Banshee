using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.utils;

namespace Banshee.packets
{
    public class x46PONGTOHOST : IPacket
    {
        public int nonce;
        public IPacket parse(BinaryReader br){
            x46PONGTOHOST p = new x46PONGTOHOST();
            p.nonce = br.ReadInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(BitConverter.GetBytes(nonce));
            return d.ToArray();
        }
    }
}