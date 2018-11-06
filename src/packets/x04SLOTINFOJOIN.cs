using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;
using Banshee.Ingame;

namespace Banshee.Packets
{
    public class x04SLOTINFOJOIN : IPacket
    {
        public uint randomseed;
        public Slot[] slots;
        public byte layoutStyle;
        public byte playerSlots;
        public ushort port;
        public uint ip;
        public byte pid;

        public IPacket parse(BinaryReader br, int len){
            x04SLOTINFOJOIN p = new x04SLOTINFOJOIN();
            int slotinfolen = br.ReadUInt16();
            int slotcount = br.ReadByte();
            for (int i = 0; i < slotcount; i++)
            {
                p.slots[i] = new Slot(br);   
            }
            p.randomseed = br.ReadUInt32();
            p.layoutStyle = br.ReadByte();
            p.playerSlots = br.ReadByte();
            p.pid = br.ReadByte();
            br.ReadByte(); //2
            br.ReadByte(); //0
            p.port = br.ReadUInt16();
            p.ip = br.ReadUInt32();
            br.ReadUInt32(); //0
            br.ReadUInt32(); //0
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            var slinfo = new List<byte>();
            slinfo.Add((byte)this.slots.Length);
            foreach (Slot slot in slots)
            {
                slinfo.AddRange(slot.AsBytes());
            }         
            slinfo.AddRange(BitConverter.GetBytes(this.randomseed));
            slinfo.Add(this.layoutStyle);
            slinfo.Add(this.playerSlots);
            d.AddRange(BitConverter.GetBytes((ushort)slinfo.Count()));
            d.AddRange(slinfo);
            d.Add(this.pid);
            d.Add(0x02);
            d.Add(0x00);
            d.AddRange(BitConverter.GetBytes(this.port));
            d.AddRange(BitConverter.GetBytes(this.ip));
            d.AddRange(BitConverter.GetBytes((int)0x0));
            d.AddRange(BitConverter.GetBytes((int)0x0));
            return d.ToArray();
        }
    }
}