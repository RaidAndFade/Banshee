using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Text;
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
        GameState State;

        Banshee p;

        Thread lobbyThread;

        public uint id = 1;
        uint entryKey;

        Player[] players = new Player[12];

        IPEndPoint broadcastAddr;

        Map gameMap;

        public Game(Banshee _p, string mappath){
            p = _p;
            entryKey = (uint)new Random().Next();
            State = GameState.LOBBY;

            gameMap = new Map(mappath);

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
            pkt.stats = GetStatStringData();
            pkt.slots = 24;
            pkt.gameflags = 9;
            pkt.players = 0;
            pkt.freeslots = 24;
            pkt.age = 0;
            pkt.port = 6112;
            
            System.Console.WriteLine(string.Join('-',pkt.stats));
            p.sendUDPPacket(pkt,to);
        }

        public byte[] GetStatStringData(){
            StatStringData ssd = gameMap.GetStatString();

            ssd.hostname = "abc";

            return StatStringData.ToBytes(ssd);
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