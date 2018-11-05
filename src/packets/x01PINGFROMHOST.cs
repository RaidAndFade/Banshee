using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.utils;

namespace Banshee.packets
{
    public class x01PINGFROMHOST : IPacket
    {
        public int nonce;
        public IPacket parse(BinaryReader br){
            x01PINGFROMHOST p = new x01PINGFROMHOST();
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