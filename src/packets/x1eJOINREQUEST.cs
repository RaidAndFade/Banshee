using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.utils;

namespace Banshee.packets
{
    public class x1eJOINREQUEST : IPacket
    {
        public int gameId;
        public int entryKey;
        public byte unk;
        public short port;
        public int peerkey;
        public string name;
        public int peerdata;
        public short iport;
        public int iip;

        public IPacket parse(BinaryReader br){
            x1eJOINREQUEST p = new x1eJOINREQUEST();
            p.gameId = br.ReadInt32();
            p.entryKey = br.ReadInt32();
            p.unk = br.ReadByte();
            p.port = br.ReadInt16();
            p.peerkey = br.ReadInt32();
            p.name = ConvertUtils.parseStringZ(br);
            p.peerdata = br.ReadInt32();
            p.iport = br.ReadInt16();
            p.iip = br.ReadInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(BitConverter.GetBytes(gameId));
            d.AddRange(BitConverter.GetBytes(entryKey));
            d.Add(unk);
            d.AddRange(BitConverter.GetBytes(port));
            d.AddRange(BitConverter.GetBytes(peerkey));
            d.AddRange(ConvertUtils.fromStringZ(name));
            d.AddRange(BitConverter.GetBytes(gameId));
            d.AddRange(BitConverter.GetBytes(iport));
            d.AddRange(BitConverter.GetBytes(iip));
            return d.ToArray();
        }
    }
}