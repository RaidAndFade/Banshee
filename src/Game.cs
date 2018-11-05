using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Linq;
using System.Net;
using System.IO;
using System;

using Banshee.packets;
using Banshee.ingame;
using Banshee.utils;

namespace Banshee
{
    public class Game
    {

#region main init & vardec
        GameState State;

        Banshee p;

        Thread lobbyThread;
        Thread tcpThread;
        Thread gameThread;

        TcpListener tcpServer;

        public uint id = 1;
        uint entryKey;
        ConcurrentBag<PotentialPlayer> potentialPlayers = new ConcurrentBag<PotentialPlayer>();
        ConcurrentBag<ConnectedPlayer> players = new ConcurrentBag<ConnectedPlayer>();

        public Slot[] slots;

        IPEndPoint broadcastAddr;

        public Map gameMap;

        bool needToUpdateSlots;
        int lastSlotUpdate = 0;

        byte fakeHostPID = 255;

        public int ticks = 0;
        public int lastPing = 0;

        public int tickrate = 1000/60; //30tps

        public Game(Banshee _p, string mappath){
            p = _p;
            entryKey = (uint)new Random().Next();
            State = GameState.LOBBY;

            gameMap = new Map(mappath);
            slots = gameMap.Slots.ToArray();

            broadcastAddr = new IPEndPoint(IPAddress.Broadcast, 6112);
            tcpServer = new TcpListener(IPAddress.Any, 6112);
            tcpServer.Start();

            x31CREATEGAME pkt = new x31CREATEGAME();
            pkt.product = "W3XP";
            pkt.version = 30;
            pkt.gameId = id;
            p.sendUDPPacket(pkt,broadcastAddr);

            lobbyThread = new Thread(refreshThread);
            lobbyThread.Start();        

            tcpThread = new Thread(listenTCP);
            tcpThread.Start();

            gameThread = new Thread(tickloop);
            gameThread.Start();
        }

        ~Game(){
            Console.WriteLine("[x] Closing GameServer");
            tcpServer.Stop();
            tcpServer.Server.Close();
            gameThread.Abort();
            tcpThread.Abort();
            lobbyThread.Abort();
            foreach (var p in players)
            {
                p.flushSocket(); //get that last pp out
                p.pp.socket.Close();
            }
            foreach(var p in potentialPlayers)
            {
                p.socket.Close();
            }
            Console.WriteLine("[x] Closed GameServer");
        }
#endregion

#region tcp new
        public void listenTCP(){
            while(true){
                try{
                    TcpClient c = tcpServer.AcceptTcpClient();

                    handleNewClient(c);
                }catch(Exception e){
                    System.Console.WriteLine(e);
                }
            }
        }

        public async void handleNewClient(TcpClient c){
            NetworkStream ns = c.GetStream();
            byte[] buf = new byte[1024];
            ns.Read(buf,0,buf.Length);
            if(buf[0] != 0xF7 || buf[1] != 0x1e) {
                c.Close();
                return;
            }

            using(BinaryReader br = new BinaryReader(new MemoryStream(buf.Skip(4).ToArray()))){
                x1eJOINREQUEST jr = (x1eJOINREQUEST)(new x1eJOINREQUEST().parse(br));
                PotentialPlayer pp = new PotentialPlayer(this,c,ns,jr);
                if(isValidJr(jr)){
                    this.potentialPlayers.Add(pp);
                    Console.WriteLine("Now have "+this.potentialPlayers.Count()+" total pps...");
                    Console.WriteLine("PP sent Valid JR, adding to list of pps");
                }else{
                    pp.rejectJoin((int)REJECTJOIN.WRONGPASSWD); //idk man seems right
                    c.Close();
                }
            }
        }
        public bool isValidJr(x1eJOINREQUEST jr){
            Console.WriteLine("Received JoinRequest from "+jr.name+" at "+jr.iip+":"+jr.port+".");
            
            return jr.gameId == this.id
                    //jr.entryKey == keymap[jr.iip]
                    && jr.name.Length > 0 && jr.name.Length <= 15 
                    ;
                    
        }
#endregion

#region tick
        public void tickloop(){
            while(true){
                ticks++;
                tick();
                tickAfter();
                Thread.Sleep((int)(300));
            }
        }

        public void tick(){
            foreach (var pp in this.potentialPlayers)
            {   
                if(pp.shouldDelete || pp.hasError || !pp.socket.Connected){
                    continue;
                }
                if(!pp.isConnected){ //new player, handle them.
                    pp.isConnected = true;
                    
                    if(players.Count+1 >= gameMap.MapNumPlayers){
                        RemoveFakeHost();
                    }

                    byte pid = GetNextPID();
                    byte sid = GetOpenSlotID();


                    if(sid == 0xff || pid == 0xff){ //open slot not found, open pid not found
                        pp.rejectJoin((int)REJECTJOIN.FULL);
                        return;
                    }

                    slots[sid].pid = pid;
                    slots[sid].slotStatus = (byte)SlotStatus.OCCUPIED;
                    slots[sid].computer = 0;
                    UpdateSlots();

                    var pl = new ConnectedPlayer(pp,pid,pp.joinReq.name);
                    players.Add(pl);
                    
                    //SENDING SLOTINFOJOIN
                    x04SLOTINFOJOIN p = new x04SLOTINFOJOIN();
                    p.slots = this.slots;
                    p.port = (ushort)((IPEndPoint) pp.socket.Client.RemoteEndPoint).Port; //could also do pp.joinreq.port and pp.joinreq.ip
                    p.ip = (uint)((IPEndPoint) pp.socket.Client.RemoteEndPoint).Address.Address; //this gives warning no matter what apparently.
                    p.pid = pid;
                    p.playerSlots = (byte)gameMap.MapNumPlayers;
                    p.layoutStyle = gameMap.GetMapLayoutStyle();
                    p.randomseed = 0xefbeadde; //random, lol.
                    pp.player = pl;
                    pl.queuePacket(p);

                    //SENDING MAPCHECK
                    x3dMAPCHECK MCp = new x3dMAPCHECK();
                    MCp.mapPath = this.gameMap.MapPath;
                    MCp.mapCRC = this.gameMap.MapCRC;
                    MCp.mapInfo = this.gameMap.MapInfo;
                    MCp.mapSHA1 = this.gameMap.MapSha;
                    MCp.mapSize = this.gameMap.MapSize;
                    pl.queuePacket(MCp);

                    //SENDING FAKEHOST INFO (IF EXISTS)
                    if(fakeHostPID != 255){
                        pl.queuePacket(GetPlayerInfo(fakeHostPID));
                    }

                    var plinfo = GetPlayerInfo(pl);
                    
                    //SENDING OTHER PLAYERS
                    foreach (var otherplayer in players)
                    {
                        if(otherplayer == pl) continue;

                        pl.queuePacket(GetPlayerInfo(otherplayer));
                        otherplayer.queuePacket(plinfo);
                    }

                    x01PINGFROMHOST pingpkt = new x01PINGFROMHOST();
                    pingpkt.nonce = (int)(GetTimeMS()%1000000); //this is hacky, its described better in the Player ping receive handler
                    pl.queuePacket(pingpkt); //send the first ping packet along with the other shit. if this works we've successfully meme'd GHost++
                }
            }

            if( /*gameState == LOBBY && */ fakeHostPID == 255 && players.Count < gameMap.MapNumPlayers )
                CreateFakeHost();


            foreach(ConnectedPlayer p in this.players){
                if(p.pp.shouldDelete || p.pp.hasError || !p.pp.socket.Connected) continue;
                p.tick();
            }

            ConnectedPlayer[] badplayers = players.Where(p => p.pp.shouldDelete || p.pp.hasError || !p.pp.socket.Connected).ToArray();
            for(var x=0;x<badplayers.Length;x++)
            {
                RemovePlayer(badplayers[x], null);
                Console.WriteLine("*** Attempting to remove "+badplayers[x].name);
                if(this.potentialPlayers.TryTake(out badplayers[x].pp) && this.players.TryTake(out badplayers[x])){
                    badplayers[x] = null;
                    Console.WriteLine("Removed player because either error,left,or dc.");
                }else{
                    Console.WriteLine("Could not remove player "+badplayers[x].name+"!");
                }
            }

            if(needToUpdateSlots){
                //implement mechanism so that download % is only constantly updated on the person downloading, and once per second on everyone else.
                // in theory could be simple as long as i make another flag "slotDLStatusUpdate" and set it to true if there's a download change
                needToUpdateSlots = false;
                Console.WriteLine("Sending updated slot info as requested");
                x09SLOTINFO sinfo = new x09SLOTINFO();
                sinfo.slots = slots;
                sinfo.playerSlots = (byte)gameMap.MapNumPlayers;
                sinfo.layoutStyle = gameMap.GetMapLayoutStyle();
                sinfo.randomseed = 0xdeadbeef;
                SendAll(sinfo);
            }
        }

        public void tickAfter(){
            if(lastPing < GetTime()-5){
                lastPing = GetTime();
                x01PINGFROMHOST pingpkt = new x01PINGFROMHOST();
                pingpkt.nonce = (int)(GetTimeMS()%1000000); //hacky, explained in player
                SendAll(pingpkt);
            } 
            //every 5 seconds send a ping (make this 1-2 seconds if ingame)

            foreach (var p in this.players)
            {   
                if(p.pp.shouldDelete || p.pp.hasError || !p.pp.socket.Connected) continue;
                if(p.netwritebuff.Count==0) continue;
                Console.WriteLine("Flushing "+p.pid);
                p.flushSocket();
            }
        }
#endregion

#region PID and SID functions
        public byte GetNextPID(){
            for (byte i = 1; i < 0xff; i++)
            {
                if(i == fakeHostPID) continue;

                bool goodid = true;
                foreach (var cp in players)
                {
                    if(cp.pid == i){
                        goodid = false;
                        break;
                    }
                }
                if(goodid) return i;
            }
            if(fakeHostPID != 255){
                RemoveFakeHost();
                return fakeHostPID;
            }
            return 0xff;
        }

        public byte GetOpenSlotID(){
            for (byte i = 0; i < slots.Length; i++){
                if(slots[i].slotStatus == (byte)SlotStatus.OPEN){
                    return i;
                }
            }
            return 0xff;
        }

#endregion

#region PlayerInfo Generators & FakeHost manipulation

        public x06PLAYERINFO GetPlayerInfo(ConnectedPlayer p){
            if(p == null) return null;
            x06PLAYERINFO pi = new x06PLAYERINFO();
            pi.pid = p.pid;
            pi.name = p.name;
            pi.externalIp = BitConverter.GetBytes((int)((IPEndPoint) p.pp.socket.Client.RemoteEndPoint).Address.Address);
            pi.internalIp = BitConverter.GetBytes(p.pp.joinReq.iip);
            return pi;
        }

        public x06PLAYERINFO GetPlayerInfo(byte pid){
            if(pid == 255) return null;
            if(pid == fakeHostPID){
                var fakeHostIp = new byte[]{0,0,0,0};
                x06PLAYERINFO fhip = new x06PLAYERINFO();
                fhip.pid = fakeHostPID;
                fhip.name = Banshee.BOTNAME;
                fhip.externalIp = fhip.internalIp = fakeHostIp;
                return fhip;
            }
            var p = GetPlayer(pid);
            return GetPlayerInfo(p);
        }

        public byte GetHostPid(){
            if(fakeHostPID != 255) return fakeHostPID;

            foreach (var p in players)
            {
                return p.pid;//meme
            }

            return 255;
        }

        public byte[] GetAllPids(bool includeFakeHost=false){
            List<byte> pidlist = new List<byte>();
            foreach (var p in players)
            {
                pidlist.Add(p.pid);
            }
            return pidlist.ToArray();
        }

        public void CreateFakeHost(){
            if(fakeHostPID != 255) return;

            fakeHostPID = GetNextPID();
            var fakeHostIp = new byte[]{0,0,0,0};
            x06PLAYERINFO fhip = new x06PLAYERINFO();
            fhip.pid = fakeHostPID;
            fhip.name = Banshee.BOTNAME;
            fhip.externalIp = fhip.internalIp = fakeHostIp;
            SendAll(fhip);
        }

        public void RemoveFakeHost(){
            if(fakeHostPID == 255) return;

            x07PLAYERLEAVE fhlp = new x07PLAYERLEAVE();
            fhlp.pid = fakeHostPID;
            fhlp.reason = (int)PLAYERLEAVEREASON.LOBBY;
            fakeHostPID = 255;
            SendAll(fhlp);
        }
#endregion

#region Event Handlers (RemovePlayer, SendAll, HandleClientChat, etc...)
        public void RemovePlayer(ConnectedPlayer p, uint? reason){
            uint r;
            if(reason.HasValue){
                r = reason.Value;
            }else{
                r = (uint)PLAYERLEAVEREASON.DISCONNECT;
            }
            var sid = GetSlotId(p);
            Console.WriteLine("User left, cleaning slot "+sid);
            if(sid != 255){
                slots[sid].slotStatus = (byte)SlotStatus.OPEN;
                slots[sid].pid = 0;
                slots[sid].downloadStatus = 255;
                UpdateSlots();
            }

            x07PLAYERLEAVE leavepkt = new x07PLAYERLEAVE();
            leavepkt.pid = p.pid;
            leavepkt.reason = r;
            SendAll(leavepkt);
        }

        public void SendTo(byte[] pids, IPacket pkt){
            foreach(var pid in pids){
                var p = GetPlayer(pid);
                if(p != null && !p.pp.hasError && !p.pp.shouldDelete){
                    p.queuePacket(pkt);
                }
            }
        }

        public void SendAll(IPacket pkt){
            foreach(var p in players){
                if(p.pp.shouldDelete || p.pp.hasError || !p.pp.socket.Connected) continue;
                p.queuePacket(pkt);
            }
        }

        public void HandleClientChat(ConnectedPlayer p, x28CHATTOHOST cc){
            if(p.pid != cc.fromPID) return;

            if(cc.command == (byte)CHATTOHOSTCOMMANDS.MESSAGE || cc.command == (byte)CHATTOHOSTCOMMANDS.MESSAGEEXTRA){
                bool hidden = false;
                string msg = (string)cc.args;

                if(cc.extra==null){ //lobby message
                    Console.WriteLine("[LOBBY] " + p.name + ": " + msg);
                }else{
                    byte[] extra = (byte[])cc.extra;
                    if(extra.Length != 4)  //malformed somehow
                        return;

                    if(extra[0] == 0){ //ingame message to ALL

                    }else if(extra[0] == 2){ //ingame message to OBSERVERS

                    }
                }

                if(msg[0] == '!'){
                    hidden = HandleClientCommand(p,msg);
                }

                if(!hidden){
                    x0fCHATFROMHOST cfh = new x0fCHATFROMHOST();
                    cfh.toPIDs = cc.toPIDs; cfh.fromPID = cc.fromPID; cfh.extra = cc.extra; cfh.command = cc.command; cfh.args = cc.args;
                    SendTo(cc.toPIDs,cfh);
                }
            }
            // if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGECOLOR) HandleClientColorChangeRequest(p,(byte)cc.args);
            // if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGETEAM) HandleClientTeamChangeRequest(p,(byte)cc.args);
            // if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGERACE) HandleClientRaceChangeRequest(p,(byte)cc.args);
            // if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGEHANDICAP) HandleClientHandicapChangeRequest(p,(byte)cc.args);

        }

        public bool HandleClientCommand(ConnectedPlayer p, string msg){
            msg = msg.Substring(1);
            Console.WriteLine("[*] Incoming Command : !"+msg);
            if(msg.Length>=5 && msg.Substring(0,5) == "echo "){
                HostSendChat(msg.Substring(5), new byte[]{p.pid});
                return true;
            }
            if(msg.Length>=4 && msg.Substring(0,4) == "ping"){
                var cping = p.getPing();
                if(cping == -1)
                    HostSendChat("There is no reliable ping for you yet.", new byte[]{p.pid});
                else
                    HostSendChat("Your ping is "+Math.Round(cping,2)+"ms.", new byte[]{p.pid});

                return true;
            }

            return false;
        }

        public void HostSendChat(string msg, byte[] toPIDs){
            //if ingame... send extramessage with extra[0]==0
            x0fCHATFROMHOST cfh = new x0fCHATFROMHOST();
            cfh.command = (byte)CHATTOHOSTCOMMANDS.MESSAGE;
            cfh.fromPID = GetHostPid();
            cfh.toPIDs = toPIDs;
            cfh.args = msg;
            cfh.extra = null;
            SendTo(toPIDs, cfh);
        }
#endregion

#region utility functions (for players)

        public void UpdateSlots(){
            this.needToUpdateSlots = true;
        }

        public ConnectedPlayer GetPlayer(int pid){
            foreach (var p in this.players)
            {
                if(p.pid == pid) return p;
            }
            return null;
        }
        public byte GetSlotId(ConnectedPlayer p){
            for(byte x=0;x<this.slots.Length;x++)
            {
                if(slots[x].pid == p.pid){
                    return x;
                }
            }
            return byte.MaxValue;
        }
        public Slot GetSlot(ConnectedPlayer p){
            foreach (var slot in this.slots)
            {
                if(slot.pid == p.pid){
                    return slot;
                }
            }
            return null;
        }

#endregion

#region udp lan
        public void refreshThread(){
            while(State == GameState.LOBBY){
                Thread.Sleep(5000);
                sendRefreshBroadcast();
            }
        }

        public void sendRefreshBroadcast(){
            x32REFRESHGAME pkt = new x32REFRESHGAME();
            pkt.gameid = id;
            pkt.numplayers = (uint)players.Count();
            pkt.slots = (uint)gameMap.MapNumPlayers;
            p.sendUDPPacket(pkt,broadcastAddr);
        }

        public void sendGameDetails(IPEndPoint to){
            x30GAMEDETAILS pkt = new x30GAMEDETAILS();
            pkt.product = "W3XP";
            pkt.gameVersion = 30;
            pkt.gameId = id;
            pkt.entryKey = entryKey;
            pkt.gameName = "|c0000ffffBig boy club";
            pkt.passwd = "";
            pkt.stats = GetStatStringData();
            pkt.slots = (uint)gameMap.MapNumPlayers;
            pkt.gameflags = 9;
            pkt.players = (uint)players.Count();
            pkt.freeslots = pkt.slots-pkt.players; //? lol
            pkt.age = 0;
            pkt.port = 6112;
            
            //System.Console.WriteLine(string.Join('-',pkt.stats));
            p.sendUDPPacket(pkt,to);
        }

        public byte[] GetStatStringData(){
            StatStringData ssd = gameMap.GetStatString();

            ssd.hostname = Banshee.BOTNAME;

            return StatStringData.ToBytes(ssd);
        }

        public byte[] getLanGameDetails(){
            var data = new List<byte>();

            return data.ToArray();
        }
#endregion
    
        public int GetTime(){
            return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        public long GetTimeMS(){
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    public enum GameState{
        LOBBY = 0x00,
        INGAME = 0x01
    }
}