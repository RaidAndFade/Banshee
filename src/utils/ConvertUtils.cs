using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Banshee.utils
{
    public class ConvertUtils
    {

        public static byte[] fromString(string s){
            return Encoding.UTF8.GetBytes(s);
        }
        public static byte[] fromStringReverse(string s){
            return Encoding.UTF8.GetBytes(s).Reverse().ToArray();
        }

        public static byte[] fromStringZ(string s){
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            List<byte> d = bytes.ToList();
            d.Add(0);
            return d.ToArray();
        }

        public static string parseString(BinaryReader br, int len){
            byte[] arr = br.ReadBytes(len);
            return Encoding.UTF8.GetString(arr);
        }

        public static string parseReverseString(BinaryReader br, int len){
            byte[] arr = br.ReadBytes(len).Reverse().ToArray();
            return Encoding.UTF8.GetString(arr);
        }
        public static string parseStringZ(BinaryReader br){
            string s = "";

            byte b;
            while((b = br.ReadByte()) != 0){
                s += (char)b;
            }

            return s;
        }

        public static byte[] parseBytesZ(BinaryReader br){
            List<byte> bytes = new List<byte>();

            byte b;
            while((b = br.ReadByte()) != 0){
                bytes.Add(b);
            }

            return bytes.ToArray();
        }
    }
}