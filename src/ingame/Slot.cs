using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Banshee.Ingame
{
    public class Slot
    {
        public byte pid, downloadStatus, slotStatus, computer, team, color, race, computertype, handicap;

        public Slot(byte p=0, byte dS=255, byte sS=(byte)SlotStatus.OPEN, byte c=0, byte t=0, byte co=0, byte r=(byte)SlotRace.RANDOM,byte cT=(byte)SlotCompType.NORMAL, byte h=100){
            pid = p; downloadStatus = dS; slotStatus = sS; computer = c; team = t; color = co; race = r; computertype = cT; handicap = h;
        }
        public Slot(byte[] d){
            pid = d[0];
            downloadStatus = d[1];
            slotStatus = d[2];
            computer = d[3];
            team = d[4];
            color = d[5];
            race = d[6];
            if(d.Length>=8)
                computertype = d[7];
            if(d.Length>=9)
                handicap = d[8];
        }

        public Slot(BinaryReader br){
            this.pid = br.ReadByte();
            this.downloadStatus = br.ReadByte();
            this.slotStatus = br.ReadByte();
            this.computer = br.ReadByte();
            this.team = br.ReadByte();
            this.color = br.ReadByte();
            this.race = br.ReadByte();
            this.computertype = br.ReadByte();
            this.handicap = br.ReadByte();

        }
        public byte[] AsBytes(){
            var d = new List<byte>();
            d.Add(pid);
            d.Add(downloadStatus);
            d.Add(slotStatus);
            d.Add(computer);
            d.Add(team);
            d.Add(color);
            d.Add(race);
            d.Add(computertype);
            d.Add(handicap);
            return d.ToArray();
        }

        public override string ToString(){
            return pid + " " + downloadStatus + " " + slotStatus + " " + computer + " " + team + " " + color + " " + race + " " + computertype + " " + handicap;
        }
    }

    public enum SlotStatus : byte {
        OPEN=0,
        CLOSED=1,
        OCCUPIED=2
    }

    public enum SlotRace : byte {
        HUMAN=1,
        ORC=2,
        NIGHTELF=4,
        UNDEAD=8,
        RANDOM=32,
        SELECTABLE=64
    }

    public enum SlotCompType : byte {
        EASY=0,
        NORMAL=1,
        HARD=2
    }
}