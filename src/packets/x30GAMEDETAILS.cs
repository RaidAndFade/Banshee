using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

using Banshee.utils;

namespace Banshee.packets
{
    public class x30GAMEDETAILS:IPacket
    {
        
        public string product;
        public uint gameVersion,gameId,entryKey; 
        public string gameName,passwd;
        public byte[] stats;  
        public uint slots,gameflags,players,freeslots,age; 
        public ushort port; 

        public IPacket parse(BinaryReader br){
            x30GAMEDETAILS p = new x30GAMEDETAILS();
            p.product = ConvertUtils.parseReverseString(br,4);
            p.gameVersion = br.ReadUInt32();
            p.gameId = br.ReadUInt32();
            p.entryKey = br.ReadUInt32();
            p.gameName = ConvertUtils.parseStringZ(br);
            p.passwd = ConvertUtils.parseStringZ(br);
            p.stats = StatString.decode(ConvertUtils.parseBytesZ(br));
            p.slots = br.ReadUInt32();
            p.gameflags = br.ReadUInt32();
            p.players = br.ReadUInt32();
            p.freeslots = br.ReadUInt32();
            p.age = br.ReadUInt32();
            p.port = br.ReadUInt16();
            return p;
        }

        public byte[] toBytes(){
            var d = new List<byte>();
            d.AddRange(ConvertUtils.fromStringReverse(product));
            d.AddRange(BitConverter.GetBytes(gameVersion));
            d.AddRange(BitConverter.GetBytes(gameId));
            d.AddRange(BitConverter.GetBytes(entryKey));
            d.AddRange(ConvertUtils.fromStringZ(gameName));
            d.AddRange(ConvertUtils.fromStringZ(passwd));
            d.AddRange(StatString.encode(stats));
            d.Add(00);
            d.AddRange(BitConverter.GetBytes(slots));
            d.AddRange(BitConverter.GetBytes(gameflags));
            d.AddRange(BitConverter.GetBytes(players));
            d.AddRange(BitConverter.GetBytes(freeslots));
            d.AddRange(BitConverter.GetBytes(age));
            d.AddRange(BitConverter.GetBytes(port));
            return d.ToArray();
        }
    }
}