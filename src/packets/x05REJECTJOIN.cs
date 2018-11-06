using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x05REJECTJOIN : IPacket
    {
        public int reason;
        public IPacket parse(BinaryReader br, int len){
            x05REJECTJOIN p = new x05REJECTJOIN();
            p.reason = br.ReadInt32();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(BitConverter.GetBytes(reason));
            return d.ToArray();
        }
    }

    public enum REJECTJOIN:int{
        FULL = 9,
        STARTED = 10,
        WRONGPASSWD = 27
    }
}