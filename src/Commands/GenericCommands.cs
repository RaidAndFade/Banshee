using System;

using Banshee.Ingame;

namespace Banshee.Commands{
    public class EchoCommand : ICommand{
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            if(msg[0] == ' ') msg = msg.Substring(1);
            if(msg.Substring(0,4) == "echo") msg = msg.Substring(4);
            if(msg[0] == ' ') msg = msg.Substring(1);
            return new CommandResponse(msg,true,true);
        }
    }
    public class PingCommand : ICommand{
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            double ping = p.GetPing();
            string resp;
            if(ping == -1)
                resp = "There is no reliable ping for you yet.";
            else
                resp = "Your ping is "+Math.Round(ping,2)+"ms.";
            return new CommandResponse(resp,true,true);
        }
    }
}