using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;

namespace Banshee.Packets
{
    public class x26CLIENTACTION : IPacket
    {
        public byte[] crc;
        public byte[] action;
        public IPacket parse(BinaryReader br, int len){
            x26CLIENTACTION p = new x26CLIENTACTION();
            p.crc = br.ReadBytes(4);
            p.action = br.ReadBytes(len-4); //read everything else :)
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(crc);
            d.AddRange(action);
            return d.ToArray();
        }
    }

    public struct GameAction{
        public byte pid;
        public byte[] crc;
        public byte[] action;

        public GameAction(byte pid, byte[] crc, byte[] action){
            this.pid = pid;
            this.crc = crc;
            this.action = action;
        }

        public int GetLength(){
            return action.Length + 3;//3 because idek man
        }
    }
}