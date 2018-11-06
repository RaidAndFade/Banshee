using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.Utils;
using Banshee.Ingame;

namespace Banshee.Packets
{
    public class x09SLOTINFO : IPacket
    {
        public uint randomseed;
        public Slot[] slots;
        public byte layoutStyle;
        public byte playerSlots;

        public IPacket parse(BinaryReader br, int len){
            x09SLOTINFO p = new x09SLOTINFO();
            int slotinfolen = br.ReadUInt16();
            int slotcount = br.ReadByte();
            for (int i = 0; i < slotcount; i++)
            {
                p.slots[i] = new Slot(br);   
            }
            p.randomseed = br.ReadUInt32();
            p.layoutStyle = br.ReadByte();
            p.playerSlots = br.ReadByte();
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
            return d.ToArray();
        }
    }
}