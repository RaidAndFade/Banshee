using System;
using System.IO;
using System.Collections.Generic;

using Banshee;
using Banshee.Ingame;

namespace Banshee.Commands {
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
    public class Commands{

        public static Dictionary<string,ICommand> commList = new Dictionary<string, ICommand>();
        public static void InitCommands(){
            //commList.Add();
        }
        public static CommandResponse HandleCommand(Game g, ConnectedPlayer p, string msg){
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
            if(msg.Length>=5 && msg.Substring(0,5) == "start"){
                StartGame();
            }

            return false;
        }
    }
}