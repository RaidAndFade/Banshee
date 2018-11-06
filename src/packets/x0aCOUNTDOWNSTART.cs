using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x0aCOUNTDOWNSTART : IPacket
    {
        public IPacket parse(BinaryReader br, int len){
            x0aCOUNTDOWNSTART p = new x0aCOUNTDOWNSTART();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            return d.ToArray();
        }
    }
}