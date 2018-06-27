using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.utils;

namespace Banshee.packets
{
    public struct x31CREATEGAME:IPacket
    {
        public string product;
        public uint version, gameId;
        public IPacket parse(BinaryReader br){
            x31CREATEGAME p = new x31CREATEGAME();
            p.product = ConvertUtils.parseReverseString(br,4);
            p.version = br.ReadUInt32();
            p.gameId = br.ReadUInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(ConvertUtils.fromStringReverse(product));
            d.AddRange(BitConverter.GetBytes(version));
            d.AddRange(BitConverter.GetBytes(gameId));
            return d.ToArray();
        }
    }
}