using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x08OTHERGAMELOADED : IPacket
    {
        public byte pid;

        public IPacket parse(BinaryReader br, int len){
            x08OTHERGAMELOADED p = new x08OTHERGAMELOADED();
            p.pid=br.ReadByte();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.Add(pid);
            return d.ToArray();
        }
    }
}