using System.Collections.Generic;

namespace WC3_PROTOCOL.utils
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

    public struct StatsStringData{
        int mapflags;
        short mapwidth;
        short mapheight;
        int mapcrc;
        string mappath;
        string hostname;

        byte[] random_shit;

    }
}