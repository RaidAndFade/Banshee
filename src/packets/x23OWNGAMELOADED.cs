using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x23OWNGAMELOADED : IPacket
    {
        public IPacket parse(BinaryReader br, int len){
            x23OWNGAMELOADED p = new x23OWNGAMELOADED();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            return d.ToArray();
        }
    }
}