using System;
using System.IO;
using System.Collections.Generic;

using Banshee;
using Banshee.Ingame;

namespace Banshee.Commands {
    public class CommandHandler {

        public static Dictionary<string,ICommand> commList = new Dictionary<string, ICommand>();
        public static void InitCommands(){
            commList["start"] =  new StartGameCommand();
            commList["echo"] =  new EchoCommand();
            commList["ping"] =  new PingCommand();
            commList["computer"] = new ComputerCommand();
            commList["team"] = new ChangeTeamCommand();
            commList["color"] = new ChangeColorCommand();
            commList["race"] = new ChangeRaceCommand();
            commList["handicap"] = new ChangeHandicapCommand();
        }
        public static CommandResponse? HandleCommand(Game g, ConnectedPlayer p, string msg){
            msg = msg.Substring(1);
            Console.WriteLine("[*] Incoming Command : !"+msg);

            var c = msg.Split(" ")[0].ToLower();
            if(commList.ContainsKey(c)){
                return commList[c].OnCall(g,p,msg);
            }

            return null;
        }
    }
    public struct CommandResponse{
        public bool isCommandHidden;
        public bool isResponsePrivate;
        public string response;
        public CommandResponse(string r, bool h, bool p){
            isCommandHidden = h;
            isResponsePrivate = p;
            response = r;
        }
    }
}