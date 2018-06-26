using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Net;
using System.IO;
using System;

using WC3_PROTOCOL.packets;
using WC3_PROTOCOL.ingame;

namespace WC3_PROTOCOL
{
    public class Game
    {
        GameState State;

        Program p;

        Thread lobbyThread;

        public uint id = 1;
        uint entryKey;

        Player[] players = new Player[12];

        IPEndPoint broadcastAddr;

        public Game(Program _p){
            p = _p;
            entryKey = (uint)new Random().Next();
            State = GameState.LOBBY;

            broadcastAddr = new IPEndPoint(IPAddress.Broadcast, 6112);

            x31CREATEGAME pkt = new x31CREATEGAME();
            pkt.product = "W3XP";
            pkt.version = 29;
            pkt.gameId = id;
            p.sendUDPPacket(pkt,broadcastAddr);

            lobbyThread = new Thread(refreshThread);
            lobbyThread.Start();            
        }

        public void refreshThread(){
            while(State == GameState.LOBBY){
                Thread.Sleep(5000);
                sendRefreshBroadcast();
            }
        }

        public void sendRefreshBroadcast(){
            x32REFRESHGAME pkt = new x32REFRESHGAME();
            pkt.gameid = id;
            pkt.numplayers = 0;
            pkt.slots = 24;
            p.sendUDPPacket(pkt,broadcastAddr);
        }

        public void sendGameDetails(IPEndPoint to){
            x30GAMEDETAILS pkt = new x30GAMEDETAILS();
            pkt.product = "W3XP";
            pkt.gameVersion = 29;
            pkt.gameId = id;
            pkt.entryKey = entryKey;
            pkt.gameName = "Big boy club";
            pkt.passwd = "";
            pkt.stats = new byte[]{2,72,6,0,0,128,0,128,0,79,3,27,204,77,97,112,115,47,40,51,41,73,115,108,101,79,102,68,114,101,97,100,46,119,51,109,0,97,98,99,0,0,81,78,108,97,133,118,99,96,158,76,43,217,177,72,66,60,155,169,169,101};//this might cause issues :^)
            pkt.slots = 24;
            pkt.gameflags = 9;
            pkt.players = 0;
            pkt.freeslots = 24;
            pkt.age = 0;
            pkt.port = 6112;
            p.sendUDPPacket(pkt,to);
        }

        public byte[] getLanGameDetails(){
            var data = new List<byte>();

            return data.ToArray();
        }

        public void join(){

        }
    }

    public enum GameState{
        LOBBY = 0x00,
        INGAME = 0x01
    }
}