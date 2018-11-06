using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x07PLAYERLEAVE : IPacket
    {
        public byte pid;
        public uint reason;
        public IPacket parse(BinaryReader br, int len){
            x07PLAYERLEAVE p = new x07PLAYERLEAVE();
            p.pid = br.ReadByte();
            p.reason = br.ReadUInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.Add(pid);
            d.AddRange(BitConverter.GetBytes(reason));
            return d.ToArray();
        }
    }

    public enum PLAYERLEAVEREASON:int{
        DISCONNECT = 1,
        LOST = 7,
        LOSTBUILDINGS = 8,
        WON = 9,
        DRAW = 10,
        OBSERVER = 11,
        LOBBY = 13
    }
}