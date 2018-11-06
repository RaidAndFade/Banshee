using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x0bCOUNTDOWNEND : IPacket
    {
        public IPacket parse(BinaryReader br, int len){
            x0bCOUNTDOWNEND p = new x0bCOUNTDOWNEND();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            return d.ToArray();
        }
    }
}