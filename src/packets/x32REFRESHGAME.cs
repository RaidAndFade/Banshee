using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.utils;

namespace Banshee.packets
{
    public class x32REFRESHGAME:IPacket
    {
        public uint gameid,numplayers,slots;
        public IPacket parse(BinaryReader br){
            x32REFRESHGAME p = new x32REFRESHGAME();
            p.gameid = br.ReadUInt32();
            p.numplayers = br.ReadUInt32();
            p.slots = br.ReadUInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(BitConverter.GetBytes(gameid));
            d.AddRange(BitConverter.GetBytes(numplayers));
            d.AddRange(BitConverter.GetBytes(slots));
            return d.ToArray();
        }
    }
}