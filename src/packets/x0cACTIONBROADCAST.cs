using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Nito.KitchenSink.CRC;

using Banshee.Utils;

namespace Banshee.Packets
{
    public struct x0cACTIONBROADCAST : IPacket
    {
        public GameAction[] actions;
        public short sendInterval;
        public IPacket parse(BinaryReader br, int len){
            x0cACTIONBROADCAST p = new x0cACTIONBROADCAST();
            // p.product = ConvertUtils.parseReverseString(br,4);
            // p.version = br.ReadUInt32();
            // p.gameId = br.ReadUInt32();
            throw new Exception("x0cACTIONBROADCAST.parse() is Not implemented");
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(BitConverter.GetBytes(sendInterval));

            if(actions.Length > 0){
                var actionData = new List<byte>();

                for(var x=0;x<actions.Length;x++){
                    var ac = actions[x];
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