using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.utils;

namespace Banshee.packets
{
    public struct x42MAPSIZEVERIFY : IPacket
    {
        public byte[] mapSize;
        public byte sizeFlag;
        public IPacket parse(BinaryReader br){
            x42MAPSIZEVERIFY p = new x42MAPSIZEVERIFY();
            var unk = br.ReadBytes(4); //1 0 0 0
            p.sizeFlag = br.ReadByte();
            p.mapSize = br.ReadBytes(4);
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(new byte[]{1,0,0,0});
            d.Add(sizeFlag);
            d.AddRange(mapSize);
            return d.ToArray();
        }
    }
}