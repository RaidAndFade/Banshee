using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;
using Banshee.Ingame;

namespace Banshee.Packets
{
    public class x0fCHATFROMHOST : IPacket
    {
        public byte fromPID;
        public byte[] toPIDs;
        public byte command; 
        public Object args;
        public Object extra;

        public IPacket parse(BinaryReader br, int len){
            x0fCHATFROMHOST p = new x0fCHATFROMHOST();
            byte recipientCount = br.ReadByte();
            p.toPIDs = new byte[recipientCount];
            for(var x=0;x<recipientCount;x++){
                p.toPIDs[x] = br.ReadByte();
            }
            p.fromPID = br.ReadByte();
            p.command = br.ReadByte();
            switch (p.command)
            {
                case (byte)CHATTOHOSTCOMMANDS.CHANGETEAM:
                case (byte)CHATTOHOSTCOMMANDS.CHANGECOLOR:
                case (byte)CHATTOHOSTCOMMANDS.CHANGERACE:
                case (byte)CHATTOHOSTCOMMANDS.CHANGEHANDICAP:
                    p.args = br.ReadByte();
                    break;

                case (byte)CHATTOHOSTCOMMANDS.MESSAGE:
                    p.args = ConvertUtils.parseStringZ(br);
                    break;

                case (byte)CHATTOHOSTCOMMANDS.MESSAGEEXTRA:
                    p.extra = br.ReadBytes(4);
                    p.args = ConvertUtils.parseStringZ(br);
                    break;
            }
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.Add((byte)toPIDs.Count());
            foreach (var pid in toPIDs)
            {
                d.Add(pid);
            }
            d.Add(fromPID);
            d.Add(command);
            switch (command)
            {
                case (byte)CHATTOHOSTCOMMANDS.CHANGETEAM:
                case (byte)CHATTOHOSTCOMMANDS.CHANGECOLOR:
                case (byte)CHATTOHOSTCOMMANDS.CHANGERACE:
                case (byte)CHATTOHOSTCOMMANDS.CHANGEHANDICAP:
                    d.Add((byte)args);
                    break;
                    
                case (byte)CHATTOHOSTCOMMANDS.MESSAGEEXTRA:
                    d.AddRange((byte[])extra);
                    d.AddRange(ConvertUtils.fromStringZ((string)args));
                    break;
                    
                case (byte)CHATTOHOSTCOMMANDS.MESSAGE:
                    d.AddRange(ConvertUtils.fromStringZ((string)args));
                    break;
            }
            return d.ToArray();
        }
    }
}