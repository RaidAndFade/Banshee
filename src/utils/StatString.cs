using System;
using System.IO;
using System.Collections.Generic;

namespace Banshee.utils
{
    public class StatString
    {
        public static byte[] encode(byte[] b){
            List<byte> res = new List<byte>();

            byte m = 1;

            for(int i=0; i<b.Length; ++i){
                if(b[i]%2 == 0){
                    res.Add((byte)(b[i]+1));
                }else{
                    res.Add((byte)(b[i]));
                    m |= (byte)(1 << ((i%7)+1));
                }

                if( i % 7 == 6 || i == b.Length - 1){
                    res.Insert(res.Count - 1 - (i % 7), m);
                    m = 1;
                }
            }

            return res.ToArray();
        }

        public static byte[] decode(byte[] b){
            List<byte> res = new List<byte>();
            byte m = 1;

            for(int i=0; i<b.Length; ++i){
                if( i % 8 == 0){
                    m = b[i];
                }else{
                    if( ( m & ( 1 << i % 8 ) ) == 0){
                        res.Add((byte)(b[i] - 1));
                    }else{
                        res.Add(b[i]);
                    }
                }
            }

            return res.ToArray();
        }
    }
    //01-B3-51-4F-6D-61-85-77-63-71-61-9F-4D-2B-D9-B1-49-79-43-3D-9B-A9-A9-65
    //00 51 4e 6c 61 85 76 63 60 9e 4c 2b d9 b1 48 42 3c 9b a9 a9 65

    public struct StatStringData{
        public int mapflags;
        public short mapwidth;
        public short mapheight;
        public byte[] mapcrc;
        public string mappath;
        public string hostname;

        public byte[] mapsha;

        public static StatStringData FromBytes(byte[] d){
            if(d.Length < 4+2+2+4+20+2) throw new Exception("Data Mismatch, bytes provided too small");

            BinaryReader br = new BinaryReader(new MemoryStream(d));

            StatStringData s = new StatStringData();
            s.mapflags = br.ReadInt32();
            s.mapwidth = br.ReadInt16();
            s.mapheight = br.ReadInt16();
            s.mapcrc = br.ReadBytes(4);
            s.mappath = ConvertUtils.parseStringZ(br);
            s.hostname = ConvertUtils.parseStringZ(br);
            s.mapsha = br.ReadBytes(20);

            br.Dispose();

            return s;
        }

        public static byte[] ToBytes(StatStringData s){
            List<byte> d = new List<byte>();
            d.AddRange(BitConverter.GetBytes(s.mapflags));
            d.Add(0);
            d.AddRange(BitConverter.GetBytes(s.mapwidth));
            d.AddRange(BitConverter.GetBytes(s.mapheight));
            d.AddRange(s.mapcrc);
            d.AddRange(ConvertUtils.fromStringZ(s.mappath));
            d.AddRange(ConvertUtils.fromStringZ(s.hostname));
            d.Add(0);
            d.AddRange(s.mapsha);
            return d.ToArray();
        }

    }
}