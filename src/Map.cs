using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Generic;

using Nito.KitchenSink.CRC;

using Foole.Mpq;

using Banshee.Ingame;
using Banshee.Utils;

namespace Banshee
{
    public class Map
    {
        public const byte MAX_SLOTS = 24;
        
        public List<Slot> Slots = new List<Slot>();
        
        public string MapPath;        
        public byte[] MapSize, MapInfo, MapCRC, MapSha;

        public int MapOptions, MapWidth, MapHeight, MapNumPlayers, MapNumTeams;

        public int MapSpeed, MapVisibility, MapObservers, MapFlags, MapFilterMaker, MapFilterType, MapFilterSize, MapFilterObservers;

#region initmap (load from mpq)
        public Map(string filename)
        {
            MapPath = filename;
            using (FileStream fs = new FileStream(filename, FileMode.Open)) {
                byte[] d = new byte[fs.Length];
                fs.Read(d, 0, d.Length);
                fs.Seek(0,SeekOrigin.Begin);

                this.MapSize = BitConverter.GetBytes((int)fs.Length);
                this.MapInfo = new CRC32().ComputeHash(d, 0, d.Length);

                using (MpqArchive a = new MpqArchive(fs,true))
                {
                    uint CRCVal = 0;
                    
                    SHA1Managed sha = new SHA1Managed();

                    byte[] commonJData, blizzardJData;
                    if(a.FileExists("Scripts\\common.j")){
                        using(var commonOverload = a.OpenFile("Scripts\\common.j")){
                            var ms = new MemoryStream();
                            commonOverload.CopyTo(ms);
                            commonJData = ms.ToArray();
                            ms.Dispose();
                        }
                    }else{
                        commonJData = File.ReadAllBytes("dep/common.j");
                    }
                    if(a.FileExists("Scripts\\blizzard.j")){
                        using(var blizzardOverload = a.OpenFile("Scripts\\blizzard.j")){
                            var ms = new MemoryStream();
                            blizzardOverload.CopyTo(ms);
                            blizzardJData = ms.ToArray();
                            ms.Dispose();
                        }
                    }else{
                        blizzardJData = File.ReadAllBytes("dep/blizzard.j");
                    }

                    CRCVal = CRCVal ^ XORRotateLeft(commonJData);
                    CRCVal = CRCVal ^ XORRotateLeft(blizzardJData);

                    sha.TransformBlock(commonJData,0,commonJData.Length,commonJData,0);
                    sha.TransformBlock(blizzardJData,0,blizzardJData.Length,blizzardJData,0);

                    var magicBytes = new byte[]{0x9e,0x37,0xf1,0x03};
                    uint magicInt = 0x03F1379E;

                    CRCVal = ROTL(CRCVal, 3);
                    CRCVal = ROTL(CRCVal ^ magicInt, 3);

                    sha.TransformBlock(magicBytes,0,magicBytes.Length,magicBytes,0);
                    
                    string[] filenames = { "war3map.j", "scripts\\war3map.j", "war3map.w3e", "war3map.wpm", "war3map.doo", "war3map.w3u", "war3map.w3b", "war3map.w3d", "war3map.w3a", "war3map.w3q" };
                    var foundScript = false;
                    
                    foreach (string fn in filenames)
                    {
                        if(foundScript && fn == filenames[2]) continue;

                        if(!a.FileExists(fn)) continue;

                        using(MpqStream s = a.OpenFile(fn)){
                            var ms = new MemoryStream();
                            s.CopyTo(ms);
                            var fdata = ms.ToArray();
                            ms.Dispose();

                            CRCVal = ROTL(CRCVal ^ XORRotateLeft(fdata),3);
                            sha.TransformBlock(fdata,0,fdata.Length,fdata,0);
                        }
                    }

                    MapCRC = BitConverter.GetBytes(CRCVal);

                    sha.TransformFinalBlock(new byte[]{},0,0);
                    MapSha = sha.Hash;
                    //loading actual map shit now

                    int MapFlags;

                    using(MpqStream m = a.OpenFile("war3map.w3i")){
                        BinaryReader br = new BinaryReader(m);
                        var FileFormat = br.ReadInt32();
                        if(FileFormat != 18 && FileFormat != 25){
                            throw new Exception("Unknown w3i file format "+FileFormat);
                        }

                        //most of these are practically garbage, but still fun to have :)
                        int g_NumSaves = br.ReadInt32();
                        int g_EditorVer = br.ReadInt32();
                        string g_MapName = ConvertUtils.parseStringZ(br);
                        string g_MapAuthor = ConvertUtils.parseStringZ(br);
                        string g_MapDesc = ConvertUtils.parseStringZ(br);
                        string g_MapReccomendedPlayers = ConvertUtils.parseStringZ(br);
                        byte[] g_CameraBounds = br.ReadBytes(32);
                        byte[] g_CameraBoundsExt = br.ReadBytes(16);
                        int g_MapWidth = MapWidth = br.ReadInt32();
                        int g_MapHeight = MapHeight = br.ReadInt32();
                        int g_MapFlags = MapFlags = br.ReadInt32();
                        byte g_MapGroundType = br.ReadByte();

                        bool g_TFTLoading = FileFormat == 25;
                        int g_LoadingScreenId;
                        string g_LoadingScreenPath = "";
                        if(!g_TFTLoading){
                            g_LoadingScreenId = br.ReadInt32();
                        }else{
                            g_LoadingScreenId = br.ReadInt32();
                            g_LoadingScreenPath = ConvertUtils.parseStringZ(br);
                        }

                        string g_LoadingText = ConvertUtils.parseStringZ(br);
                        string g_LoadingTitle = ConvertUtils.parseStringZ(br);
                        string g_LoadingSubTitle = ConvertUtils.parseStringZ(br);

                        int g_PrologueScreenId;
                        string g_PrologueScreenPath = "";
                        if(!g_TFTLoading){
                            g_PrologueScreenId = br.ReadInt32();
                        }else{
                            g_PrologueScreenId = br.ReadInt32();
                            g_PrologueScreenPath = ConvertUtils.parseStringZ(br);
                        }

                        string g_PrologueText = ConvertUtils.parseStringZ(br);
                        string g_PrologueTitle = ConvertUtils.parseStringZ(br);
                        string g_PrologueSubTitle = ConvertUtils.parseStringZ(br);

                        if(FileFormat == 25){
                            br.ReadBytes(4+4+4+4+1+1+1+1+4);// int fog, int fog startz, int fog endz, int fogdensity, byte fogRED, byte ffogGREEN, byte fogBLUE, byte fogALPHA, int globalWeatherId
                            
                            ConvertUtils.parseStringZ(br); // custom sound environment

                            br.ReadBytes(5); // bytes[5] {tilesetid, waterTintRED, waterTintGREEN, waterTintBLUE, waterTintALPHA}
                        }

                        int g_NumPlayers = MapNumPlayers = br.ReadInt32();
                        int ClosedSlots = 0;

                        for(int i=0; i<MapNumPlayers; i++){
                            Slot s = new Slot();

                            s.color = (byte)br.ReadInt32();
                            int status = br.ReadInt32();
                            
                            if(status == 1){
                                s.slotStatus = (byte)SlotStatus.OPEN;
                            }else if(status==2){
                                s.slotStatus = (byte)SlotStatus.OCCUPIED;
                                s.computer = 1;
                                s.computertype = (byte)SlotCompType.NORMAL;
                            }else{
                                s.slotStatus = (byte)SlotStatus.CLOSED;
                                ClosedSlots++;
                            }

                            switch(br.ReadInt32()){
                                case 1: s.race = (byte)SlotRace.HUMAN; break;
                                case 2: s.race = (byte)SlotRace.ORC; break;
                                case 3: s.race = (byte)SlotRace.UNDEAD; break;
                                case 4: s.race = (byte)SlotRace.NIGHTELF; break;
                                default: s.race = (byte)SlotRace.RANDOM; break;
                            }

                            br.ReadBytes(4);  // fixedstart
                            ConvertUtils.parseStringZ(br); //playername (could be interesting)
                            br.ReadBytes(16); // startx, starty, allylow, allyhigh

                            if(s.slotStatus != (byte)SlotStatus.CLOSED){
                                Slots.Add(s);
                            }
                        }
                        MapNumPlayers -= ClosedSlots;

                        int g_NumTeams = MapNumTeams = br.ReadInt32();

                        for(int i=0; i<MapNumTeams; i++){
                            int Flags = br.ReadInt32();
                            int PlayerMask = br.ReadInt32();

                            for(int si=0; si < MAX_SLOTS; si++){
                                if((PlayerMask & 1) == 1){
                                    foreach (Slot s in Slots){
                                        if(s.color == si){
                                            s.team = (byte)i;
                                        }
                                    }
                                }

                                PlayerMask >>= 1;
                            }

                            ConvertUtils.parseStringZ(br); //Team Name
                        }
                    }

                    MapOptions = MapFlags & ((int)MAPOPTIONS.MELEE | (int)MAPOPTIONS.FIXEDPLAYERSETTINGS | (int)MAPOPTIONS.CUSTOMFORCES);
                    MapFilterType = (int)MAPFILTERTYPE.SCENARIO;

                    if((MapOptions & (int)MAPOPTIONS.MELEE) == (int)MAPOPTIONS.MELEE){
                        byte team = 0;
                        foreach(Slot s in Slots){
                            s.team = team++;
                            s.race = (byte)SlotRace.RANDOM;
                        }
                        MapFilterType = (int)MAPFILTERTYPE.MELEE;
                    }

                    if((MapOptions & (int)MAPOPTIONS.FIXEDPLAYERSETTINGS) != (int)MAPOPTIONS.FIXEDPLAYERSETTINGS){
                        foreach(Slot s in Slots){
                            s.race = (byte)(s.race | (byte)SlotRace.SELECTABLE);
                        }
                    }
                }
            }

            LoadGameVariables();

            if((MapFlags & (int)MAPFLAGS.RANDOMRACES) == (int)MAPFLAGS.RANDOMRACES){
                foreach(Slot s in Slots){
                    s.race = (byte)SlotRace.RANDOM;
                }
            }

            if((MapObservers & (int)MAPOBSERVERS.ALLOWED) == (int)MAPOBSERVERS.ALLOWED || (MapObservers & (int)MAPOBSERVERS.REFEREES) == (int)MAPOBSERVERS.REFEREES){
                while(Slots.Count < MAX_SLOTS){
                    Slots.Add(new Slot(0,255,(byte)SlotStatus.OPEN,0,MAX_SLOTS,MAX_SLOTS,(byte)SlotRace.RANDOM));
                }
            }

            // System.Console.WriteLine(BitConverter.ToString(MapInfo).Replace("-",""));
            // System.Console.WriteLine(BitConverter.ToString(MapCRC).Replace("-",""));
            // System.Console.WriteLine(BitConverter.ToString(MapSha).Replace("-",""));
        }
        public uint ROTL(uint i,int c){
            return (i << c) | (i >> (32 - c));
        }
        public uint XORRotateLeft(byte[] d){
            uint i=0;
            uint v=0;
            int l=d.Length;

            if(l>3){
                while(i<l-3){
                    v = ROTL(v ^ (((uint)d[i]) + ((uint)d[i+1]<<8) + ((uint)d[i+2]<<16) + ((uint)d[i+3]<<24)), 3);
                    i+=4;
                }
            }

            while(i<l){
                v = ROTL(v ^ d[i], 3);
                i++;
            }

            return v;
        }
#endregion

        public void LoadGameVariables(){
            MapSpeed = (int)MAPSPEED.FAST;
            MapVisibility = (int)MAPVISIBILITY.ALWAYSVISIBLE;
            MapObservers = (int)MAPOBSERVERS.REFEREES;
            MapFlags = (int)(MAPFLAGS.FIXEDTEAMS | MAPFLAGS.TEAMSTOGETHER);
            MapFilterMaker = (int)MAPFILTERMAKER.USER;
            MapFilterSize = (int)MAPFILTERSIZE.LARGE;
            MapFilterObservers = (int)MAPFILTEROBSERVERS.NONE;
        }

        public int GetGameFlags(){
            int f = 0;

            switch(MapSpeed){
                case (int)MAPSPEED.SLOW: f |= 0x00000000; break;
                case (int)MAPSPEED.NORMAL: f |= 0x00000001; break;
                case (int)MAPSPEED.FAST: f |= 0x00000002; break;
            }

            switch(MapVisibility){
                case (int)MAPVISIBILITY.HIDETERRAIN: f |= 0x00000100; break;
                case (int)MAPVISIBILITY.EXPLORED: f |= 0x00000200; break;
                case (int)MAPVISIBILITY.ALWAYSVISIBLE: f |= 0x00000400; break;
                case (int)MAPVISIBILITY.DEFAULT: f |= 0x00000800; break;
            }

            switch(MapObservers){
                case (int)MAPOBSERVERS.ONDEFEAT: f |= 0x00002000; break;
                case (int)MAPOBSERVERS.ALLOWED: f |= 0x00003000; break;
                case (int)MAPOBSERVERS.REFEREES: f |= 0x40000000; break;
            }

            if((MapFlags & (int)MAPFLAGS.TEAMSTOGETHER) != 0) f |= 0x00004000;
            if((MapFlags & (int)MAPFLAGS.FIXEDTEAMS) != 0)    f |= 0x00060000;
            if((MapFlags & (int)MAPFLAGS.SHAREUNITS) != 0)    f |= 0x01000000;
            if((MapFlags & (int)MAPFLAGS.RANDOMHERO) != 0)    f |= 0x02000000;
            if((MapFlags & (int)MAPFLAGS.RANDOMRACES) != 0)   f |= 0x04000000;

            return f;
        }

        public StatStringData GetStatString(){
            StatStringData s = new StatStringData();
            s.mapflags = GetGameFlags();
            s.mapwidth = (short)MapWidth;
            s.mapheight = (short)MapHeight;
            s.mappath = MapPath;
            s.mapcrc = MapCRC;
            s.mapsha = MapSha;
            s.hostname = "";
            return s;
        }

        public byte GetMapLayoutStyle(){
            // we skip 2 (just fixedplayersettings) because it's not possible without custom forces.

            if( (MapOptions & (int)MAPOPTIONS.CUSTOMFORCES) == 0) //bit not set
                return 0;
            
            if( (MapOptions & (int)MAPOPTIONS.FIXEDPLAYERSETTINGS) == 0) //bit not set
                return 1;
            
            return 3;
        }
    }

    public enum MAPOPTIONS { 
        HIDEMINIMAP             = 1 << 0,
        MODIFYALLYPRIORITIES    = 1 << 1,
        MELEE                   = 1 << 2,
        REVEALTERRAIN           = 1 << 4,
        FIXEDPLAYERSETTINGS     = 1 << 5,
        CUSTOMFORCES            = 1 << 6,
        CUSTOMTECHTREE          = 1 << 7,
        CUSTOMABILITIES         = 1 << 8,
        CUSTOMUPGRADES          = 1 << 9,
        WATERWAVESONCLIFFSHORES	= 1 << 11,
        WATERWAVESONSLOPESHORES = 1 << 12
    }

    public enum MAPSPEED {
        SLOW = 1,
        NORMAL = 2,
        FAST = 3
    }

    public enum MAPVISIBILITY {
        HIDETERRAIN = 1,
        EXPLORED = 2,
        ALWAYSVISIBLE = 3,
        DEFAULT = 4
    }

    public enum MAPOBSERVERS {
        NONE = 1,
        ONDEFEAT = 2,
        ALLOWED = 3,
        REFEREES = 4
    }

    public enum MAPFLAGS {
        TEAMSTOGETHER = 1,
        FIXEDTEAMS = 2,
        SHAREUNITS = 4,
        RANDOMHERO = 8,
        RANDOMRACES = 16
    }

    public enum MAPFILTERMAKER {
        USER = 1,
        BLIZZARD = 2
    }

    public enum MAPFILTERTYPE {
        MELEE = 1,
        SCENARIO = 2
    }
    public enum MAPFILTERSIZE {
        SMALL = 1,
        MEDIUM = 2,
        LARGE = 4
    }

    public enum MAPFILTEROBSERVERS {
        FULL = 1,
        ONDEATH = 2,
        NONE = 4
    }
}