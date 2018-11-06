using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Nito.KitchenSink.CRC;

using Banshee.Utils;

namespace Banshee.Packets
{
    public struct x48EXTRAACTIONBROADCAST : IPacket
    {
        public List<GameAction> actions;
        public IPacket parse(BinaryReader br, int len){
            x48EXTRAACTIONBROADCAST p = new x48EXTRAACTIONBROADCAST();
            // p.product = ConvertUtils.parseReverseString(br,4);
            // p.version = br.ReadUInt32();
            // p.gameId = br.ReadUInt32();
            throw new Exception("x48EXTRAACTIONBROADCAST.parse() is Not implemented");
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(BitConverter.GetBytes((short)0));

            if(actions.Count > 0){
                var actionData = new List<byte>();

                while(actions.Count>0){
                    var ac = actions.First();
                    actions.RemoveAt(0);
                    actionData.Add(ac.pid);
                    actionData.AddRange(BitConverter.GetBytes((ushort)ac.action.Length));
                    actionData.AddRange(ac.action);
                }

                byte[] crc = new CRC32().ComputeHash(actionData.ToArray(), 0, actionData.Count);

                d.Add(crc[0]);
                d.Add(crc[1]);
                d.AddRange(actionData);
            }

            return d.ToArray();
        }
    }
}