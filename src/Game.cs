using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System.IO;
using System;

using Banshee.Packets;
using Banshee.Ingame;
using Banshee.Utils;
using Banshee.Commands;

namespace Banshee
{
    public class Game
    {

#region main init & vardec
        GameState gameState;

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
        bool isfakehostingame = false;

        public int ticks = 0;
        public int lastPing = 0;

        public int tickrate = 20; //20 ms delay = fast gamemode

        public short gameSyncCount = 0;

        public bool running=true;

        public uint randomseed;

        public List<GameAction> actionList = new List<GameAction>();

        public Game(Banshee _p, string mappath, int port = 6112){
            p = _p;
            entryKey = (uint)new Random().Next();
            gameState = GameState.LOBBY;

            gameMap = new Map(mappath);
            slots = gameMap.Slots.ToArray();

            randomseed = (uint)GetTimeMS();

            broadcastAddr = new IPEndPoint(IPAddress.Broadcast, 6112);
            tcpServer = new TcpListener(IPAddress.Any, 6112);
            tcpServer.Server.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.ReuseAddress,1);
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

        public void Close(){
            if(!running) return;
            Console.WriteLine("[x] Closing GameServer");
            foreach (var p in players)
            {
                p.flushSocket(); //get that last pp out
                p.Close();
            }
            foreach(var p in potentialPlayers)
            {
                if(p.isConnected) continue; //if they're connected, they've already been closed in the last loop.
                p.Close();
            }
            Console.WriteLine("[x] Killed all clients");
            running=false;
            try{
                tcpThread.Interrupt();
                tcpThread.Abort();
            }catch(Exception){}
            Console.WriteLine("[x] Killed all threads");
            tcpServer.Server.Disconnect(true);
            tcpServer.Stop();
            Console.WriteLine("[x] Stopped TCP Server");
            tcpServer.Server.Close(0);
            tcpServer.Server.Dispose();
            Console.WriteLine("[x] Killed TCP Server");
            Console.WriteLine("[x] Closed GameServer");
        }
#endregion

#region tcp new
        public async void listenTCP(){
            while(running){
                try{
                    Task<TcpClient> c = tcpServer.AcceptTcpClientAsync();

                    handleNewClient(await c);
                }catch(Exception e){
                    if(!running) System.Console.WriteLine("[*] TcpThread Closed Gracefully");
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
                x1eJOINREQUEST jr = (x1eJOINREQUEST)(new x1eJOINREQUEST().parse(br, 0)); //We can send len=0 here because the packet's parse function does not use the len.
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
            while(running){
                ticks++;
                tick();
                tickAfter();
                if(gameState == GameState.LOBBY || gameState == GameState.LOADING) //we dont need to try that hard while in lobby. nothing is going to happen lol
                    Thread.Sleep((int)(300));
                else
                    Thread.Sleep((int)tickrate);
            }
        }

        public void tick(){
            if(gameState == GameState.INGAME && players.Count == 0 ){
                Close();
            }

            foreach (var pp in this.potentialPlayers)
            {   
                if(pp.shouldDelete || pp.hasError || !pp.socket.Connected){
                    continue;
                }
                if(!pp.isConnected){ //new player, handle them.
                    if(gameState != GameState.LOBBY){
                        pp.rejectJoin((int)REJECTJOIN.STARTED);
                        pp.shouldDelete = true;
                    }
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

                    slots[sid].computer = 0;
                    slots[sid].color = GetUnusedColor();
                    slots[sid].pid = pid;
                    slots[sid].slotStatus = (byte)SlotStatus.OCCUPIED;
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
                    p.randomseed = randomseed; //This handles what race you spawn as
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

                    //quickly throw a ping at em to get shit rollin.
                    
                    //PING Algorithm explained in Player.cs->OnPingPacket();
                    x01PINGFROMHOST pingpkt = new x01PINGFROMHOST();
                    pingpkt.nonce = (int)(GetTimeMS()%1000000); 
                    pl.queuePacket(pingpkt);
                }
            }

            //TODO fakehost is lobby only
            if( gameState == GameState.LOBBY &&  fakeHostPID == 255 && players.Count < gameMap.MapNumPlayers )
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

            if(needToUpdateSlots && gameState == GameState.LOBBY){
                //TODO(LATER)
                //implement mechanism so that download % is only constantly updated on the person downloading, and once per second on everyone else.
                // in theory could be simple as long as i make another flag "slotDLStatusUpdate" and set it to true if there's a download change
                needToUpdateSlots = false;
                Console.WriteLine("Sending updated slot info as requested");
                CleanSlots(); //make sure they are following the rules
                x09SLOTINFO sinfo = new x09SLOTINFO();
                sinfo.slots = slots;
                sinfo.playerSlots = (byte)gameMap.MapNumPlayers;
                sinfo.layoutStyle = gameMap.GetMapLayoutStyle();
                sinfo.randomseed = randomseed;
                SendAll(sinfo);
            }

            if(gameState == GameState.INGAME){
                ProcessGameActions();
            }
        }

        public void tickAfter(){
            if(lastPing < GetTime()-5){
                lastPing = GetTime();
                x01PINGFROMHOST pingpkt = new x01PINGFROMHOST();
                pingpkt.nonce = (int)(GetTimeMS()%1000000); //hacky, explained in player
                SendAll(pingpkt);
            } 
            //every 5 seconds send a ping (TODO make this 1-2 seconds if ingame)

            foreach (var p in this.players)
            {   
                if(p.pp.shouldDelete || p.pp.hasError || !p.pp.socket.Connected) continue;
                if(p.netwritebuff.Count==0) continue;
                //Console.WriteLine("Flushing "+p.pid);
                p.flushSocket();
            }
        }
#endregion

        public void ProcessGameActions(){
            gameSyncCount++;

            if(actionList.Count == 0){
                x0cACTIONBROADCAST nullab = new x0cACTIONBROADCAST();
                nullab.actions = new GameAction[0];
                nullab.sendInterval = (short)tickrate; //meme?
                SendAll(nullab);
                return;
            }
            
            List<GameAction> subactions = new List<GameAction>();
            int subactionlen = 0;

            GameAction ac = actionList.First();
            actionList.RemoveAt(0);
            subactions.Add(ac);
            subactionlen += ac.GetLength();

            while(actionList.Count > 0){
                ac = actionList.First();
                actionList.RemoveAt(0);
                if(subactionlen + ac.GetLength() > 1452){ //1460-8(header)
                    x48EXTRAACTIONBROADCAST eab = new x48EXTRAACTIONBROADCAST();
                    eab.actions = subactions.ToList();
                    SendAll(eab);
                    subactions.Clear();
                    subactionlen = 0;
                }
                subactions.Add(ac);
                subactionlen += ac.GetLength();
            }
            x0cACTIONBROADCAST ab = new x0cACTIONBROADCAST();
            ab.actions = subactions.ToArray();
            ab.sendInterval = (short)tickrate; //meme?
            SendAll(ab);

            subactions.Clear();
        }

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

        public byte GetUnusedColor(){
            for(byte x=0;x<24;x++){
                bool isUsed = false;
                foreach(var slot in slots){
                    if(slot.slotStatus == (byte)SlotStatus.OCCUPIED && slot.color == x){
                        isUsed=true;
                        break;
                    } 
                }
                if(!isUsed){
                    return x;
                }
            }
            return 255; //this should never happen
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
             //ingame we always want the fakehost to be as invisible as possible. If it does **anything**, the client will crash.
            if(gameState == GameState.LOBBY && fakeHostPID != 255) return fakeHostPID;

            foreach (var p in players)
            {
                return p.pid;
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
            x06PLAYERINFO fhip = GetPlayerInfo(fakeHostPID);
            SendAll(fhip);
        }

        public void RemoveFakeHost(){
            if(fakeHostPID == 255) return;

            x07PLAYERLEAVE fhlp = new x07PLAYERLEAVE();
            fhlp.pid = fakeHostPID;
            fhlp.reason = (int)PLAYERLEAVEREASON.LOST;
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
        
        public void SendAllExcept(byte except, IPacket pkt){
            foreach(var p in players){
                if(p.pp.shouldDelete || p.pp.hasError) continue;
                if(p.pid == except) continue;
                p.queuePacket(pkt);
            }
        }

        public void SendAll(IPacket pkt){
            foreach(var p in players){
                if(p.pp.shouldDelete || p.pp.hasError) continue;
                p.queuePacket(pkt);
            }
        }

        public void HandleClientLeaveRequest(ConnectedPlayer p, uint reason){
            RemovePlayer(p,reason);
        } 

        public void HandleClientKeepalive(ConnectedPlayer p){

            var checksumPools = new Dictionary<int, int>(); //checksum -> number of players in that checsum
            foreach(var pl in players){
                if(pl.pp.shouldDelete || pl.pp.hasError) continue;
                if(pl.checksumArr.Count==0) return;
                var latest = pl.checksumArr.First();
                //Console.WriteLine("Player "+pl.pid + "(S:"+pl.syncCounter+") claims the current checksum is "+pl.latestChecksum);
                if(checksumPools.ContainsKey(latest)){
                    checksumPools[latest]++;
                }else{
                    checksumPools.Add(latest,1);
                }
            }


            if(checksumPools.Count > 1){ //a desync exists. this is no good
                //basically, if there is more than 80% agreement that a certain checksum is wrong, only the person(s) that are wrong will be kicked
                //otherwise, everyone is kicked.

                //first we group the good and bad players together. good = part of the largest Checksum, bad = not good.
                List<int> badPools = new List<int>();
                int badplayers = 0;
                
                var largestPool = checksumPools.Keys.First();
                var largestPoolPlayerCount = checksumPools[largestPool];
                foreach (KeyValuePair<int,int> k in checksumPools){
                    if(largestPoolPlayerCount < k.Value){
                        //here we add the current pool (with less users) to the bad pile.
                        badPools.Add(largestPool); 
                        badplayers += largestPoolPlayerCount;
                        largestPool = k.Key;
                        largestPoolPlayerCount = k.Value;
                    }else if(largestPoolPlayerCount == k.Value){
                        //both pools are bad since neither side agrees and they contain the same number of players.
                        badPools.Add(largestPool); 
                        badplayers += largestPoolPlayerCount;
                        badPools.Add(k.Key); 
                        badplayers += k.Value;

                        //set the largest pool to nothing with 0 players, in case more pools exist
                        largestPool = 0;
                        largestPoolPlayerCount = 0;
                    }
                }

                if(badPools.Count == 0){
                    return;
                }

                //Next, we check to see if the number of players in the largest pool are at least 80% of the total playercount
                if(largestPoolPlayerCount/players.Count() > 0.8){
                    Console.WriteLine("Bad checksum group found. Agreement found. Kicking bads");
                    //Kick everyone not in good group, they are desynced.
                    foreach (var pl in players)
                    {
                        if(pl.checksumArr.First() != largestPool){}
                            //RemovePlayer(pl,(uint)PLAYERLEAVEREASON.DRAW);
                    }
                }else{
                    Console.WriteLine("Bad checksum group found but no agreeance. Kicking all.");
                    //kick everyone, game is over. nobody won.
                    foreach(var pl in players){
                       // RemovePlayer(pl,(uint)PLAYERLEAVEREASON.DRAW);
                    }
                    //EndGame();
                }

                foreach (var pl in players)
                {
                    if(pl.pp.shouldDelete || pl.pp.hasError) continue;
                    pl.checksumArr.RemoveAt(0);
                }
            }
        }
        public void HandleClientAction(ConnectedPlayer p, x26CLIENTACTION pkt){
            //if this is received in lobby. kick player because they are fried.
            //if this packet is longer than 1027, something is seriously wrong with player, kick player with LOST reason
            actionList.Add(new GameAction(p.pid, pkt.crc, pkt.action));
        }   
        public void HandleClientGameLoaded(ConnectedPlayer p){
            x08OTHERGAMELOADED glpkt = new x08OTHERGAMELOADED();
            glpkt.pid = p.pid;
            SendAll(glpkt);
            bool finishedLoading = true;
            foreach(var pl in players){
                if(!pl.loaded){
                    finishedLoading = false;
                    break;
                }
            }
            if(finishedLoading){
                gameSyncCount = 0;
                gameState = GameState.INGAME;
            }
        }

        public void HandleClientChat(ConnectedPlayer p, x28CHATTOHOST cc){
            if(p.pid != cc.fromPID) return;

            if(cc.command == (byte)CHATTOHOSTCOMMANDS.MESSAGE || cc.command == (byte)CHATTOHOSTCOMMANDS.MESSAGEEXTRA){
                bool hidden = false;
                string msg = (string)cc.args;

                if(cc.extra==null){ //lobby message
                    Console.WriteLine("[LOBBY] " + p.name + ": " + msg);
                    if(msg[0] == '!'){
                        hidden = HandleClientCommand(p,msg);
                    }
                }else{
                    byte[] extra = (byte[])cc.extra;
                    if(extra.Length != 4)  //malformed somehow
                        return;
                    
                    Console.WriteLine("INGAME CHAT FROM "+extra[0]+": "+msg);

                    if(extra[0] == 0){ //ingame message to ALL
                        Console.WriteLine("[ALL] " + p.name + ": " + msg);

                    }else if(extra[0] == 2){ //ingame message to OBSERVERS
                        //Unfortunately, this does not fire when there is a single observer. Really a shame, since that observer can only talk in observer chat...
                        Console.WriteLine("[OBS] " + p.name + ": " + msg);
                    }else if(extra[0] == 3){
                        Console.WriteLine("[PM "+cc.fromPID+"->"+cc.toPIDs[0]+"] "+p.name+": "+msg);
                    }

                    if(msg[0] == '!'){
                        hidden = HandleClientCommand(p,msg,extra[0]);
                    }
                }



                if(!hidden){
                    x0fCHATFROMHOST cfh = new x0fCHATFROMHOST();
                    cfh.toPIDs = cc.toPIDs; cfh.fromPID = cc.fromPID; cfh.extra = cc.extra; cfh.command = cc.command; cfh.args = cc.args;
                    SendTo(cc.toPIDs,cfh);
                }
            }

            if(gameState != GameState.LOBBY) return; //the rest can only happen in lobby.

            if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGECOLOR) HandleClientColorChange(p,(byte)cc.args);
            if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGETEAM) HandleClientTeamChange(p,(byte)cc.args);
            if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGERACE) HandleClientRaceChange(p,(byte)cc.args);
            if(cc.command == (byte)CHATTOHOSTCOMMANDS.CHANGEHANDICAP) HandleClientHandicapChange(p,(byte)cc.args);

        }

        public void HandleClientColorChange(ConnectedPlayer player, byte color){
            Slot slot = GetSlot(player);
            foreach (var s in slots)
            {
                if(color == s.color){
                    if(s.slotStatus == (byte)SlotStatus.OCCUPIED){
                        return;
                    }else{
                        s.color = slot.color; //swap the colors
                    }
                }
            }
            slot.color = color;
            UpdateSlots();
        }

        public void HandleClientTeamChange(ConnectedPlayer player, byte team){
            Slot slot = GetSlot(player);
            slot.team = team;
            UpdateSlots();
        }
        public void HandleClientRaceChange(ConnectedPlayer player, byte race){
            Slot slot = GetSlot(player);
            slot.race = race;
            UpdateSlots();
        }

        public void HandleClientHandicapChange(ConnectedPlayer player, byte handicap){
            Slot slot = GetSlot(player);
            slot.handicap = handicap;
            UpdateSlots();
        }

        //TODO make an actual command handler class (and package possibly) with actual wrappers and handlers. rather than just a bunch of if statements.
        public bool HandleClientCommand(ConnectedPlayer p, string msg, byte channel = 0){
            CommandResponse? crp = CommandHandler.HandleCommand(this,p,msg);
            if(crp.HasValue){
                CommandResponse cr = crp.Value;
                if(cr.isResponsePrivate){
                    HostSendChat(cr.response, new byte[]{p.pid}, p.pid, channel);
                }else{
                    HostSendChat(cr.response, null, GetHostPid(), channel);
                }
                return cr.isCommandHidden;
            }else{
                return false;
            }
        }

        public void StartGame(){
            SendAll(new x0aCOUNTDOWNSTART());
            gameState = GameState.LOADING;
            if(players.Count>1){
                RemoveFakeHost(); //remove fake vhost, not necessary anymore.
            }
            SendAll(new x0bCOUNTDOWNEND());
            if(players.Count==1){ //if theres only one actual player, we've been gnomed. put that lil boy in the game rq
                isfakehostingame = true;
                x08OTHERGAMELOADED fhgl = new x08OTHERGAMELOADED();
                fhgl.pid=fakeHostPID;
                SendAll(fhgl);
            }
        }

        public void HostSendChat(string msg, byte[] toPIDs = null, byte fromPID = 255, byte ingamechannel=0){
            if(toPIDs == null){
                var pidList = new List<byte>();
                foreach (var p in players)
                {
                    pidList.Add(p.pid);
                }
                toPIDs = pidList.ToArray();
            }
            if(fromPID == 255)
                fromPID = GetHostPid();
            if(ingamechannel == 3)
                fromPID = toPIDs[0];

            x0fCHATFROMHOST cfh = new x0fCHATFROMHOST();
            if(gameState == GameState.LOBBY){
                cfh.command = (byte)CHATTOHOSTCOMMANDS.MESSAGE;
                cfh.extra = null;
            }else if(gameState == GameState.INGAME){
                cfh.fromPID = fromPID;
                cfh.command = (byte)CHATTOHOSTCOMMANDS.MESSAGEEXTRA;
                cfh.extra = new byte[]{ingamechannel,0,0,0};
            }
            cfh.fromPID = fromPID;
            cfh.toPIDs = toPIDs;
            cfh.args = msg;
            SendTo(toPIDs, cfh);
        }
#endregion

#region utility functions (for players)

        public void UpdateSlots(){
            this.needToUpdateSlots = true;
        }

        public void CleanSlots(){
            //does not work properly, but also seems like this isn't actually required. Might implement again later.

            // foreach (var slot in slots)
            // {
            //     if(slot.pid == 0 && slot.computer == 0){ //there is no player or computer
            //         if(slot.slotStatus == (byte)SlotStatus.OCCUPIED){
            //             slot.slotStatus = (byte)SlotStatus.OPEN;
            //         }
            //     }
            //     if(slot.slotStatus != (byte)SlotStatus.OCCUPIED){
            //         slot.downloadStatus = 0xff;
            //         if(gameMap.MapObservers > (byte)MAPOBSERVERS.NONE) //if the game HAS observers, the color should by default be 0x18
            //             slot.color = 0x18;
            //     }
            // }
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
            while(true){
                Thread.Sleep(5000);
                if(!running || gameState != GameState.LOBBY) break;
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
        LOADING = 0x01,
        INGAME = 0x02
    }
}