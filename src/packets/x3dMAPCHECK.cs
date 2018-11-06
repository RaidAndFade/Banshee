using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public struct x3dMAPCHECK : IPacket
    {
        public string mapPath;

        public byte[] mapSize, mapInfo, mapCRC, mapSHA1;
        public IPacket parse(BinaryReader br, int len){
            x3dMAPCHECK p = new x3dMAPCHECK();
            var unk = br.ReadBytes(4); //1 0 0 0
            p.mapPath = ConvertUtils.parseStringZ(br);
            p.mapSize = br.ReadBytes(4);
            p.mapInfo = br.ReadBytes(4);
            p.mapCRC = br.ReadBytes(4);
            p.mapSHA1 = br.ReadBytes(20);
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(new byte[]{1,0,0,0});
            d.AddRange(ConvertUtils.fromStringZ(mapPath));
            d.AddRange(mapSize);
            d.AddRange(mapInfo);
            d.AddRange(mapCRC);
            d.AddRange(mapSHA1);
            return d.ToArray();
        }
    }
}