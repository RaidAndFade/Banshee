using Banshee.Ingame;

namespace Banshee.Commands{
    public class StartGameCommand : ICommand {
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            g.StartGame();
            return null;
        }
    }

    public class ChangeTeamCommand : ICommand {
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            var mparts = msg.Split(" ");
            if(mparts.Length != 3) return null;
            
            int sid = int.Parse(mparts[1]);
            Slot slot = g.slots[sid-1];
            byte teamid = byte.Parse(mparts[2]);
            if(slot.slotStatus == (byte)SlotStatus.OCCUPIED){
                slot.team = teamid; //TODO do this on the game class with some handler to make sure the value isnt invalid
                g.UpdateSlots();
                return new CommandResponse("Set Slot "+sid+" to TEAM "+teamid,false,false);
            }else{
                return new CommandResponse("That slot is not occupied",false,false);
            }
        }
    }
    public class ChangeRaceCommand : ICommand {
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            var mparts = msg.Split(" ");
            if(mparts.Length != 3) return null;
            
            int sid = int.Parse(mparts[1]);
            Slot slot = g.slots[sid-1];
            byte raceid = byte.Parse(mparts[2]);
            if(slot.slotStatus == (byte)SlotStatus.OCCUPIED){
                slot.race = raceid; //this is just blatantly dumb. races arent even in order lol
                g.UpdateSlots();
                return new CommandResponse("Set Slot "+sid+" to RACE "+raceid,false,false);
            }else{
                return new CommandResponse("That slot is not occupied",false,false);
            }
        }
    }
    public class ChangeColorCommand : ICommand {
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            var mparts = msg.Split(" ");
            if(mparts.Length != 3) return null;
            
            int sid = int.Parse(mparts[1]);
            Slot slot = g.slots[sid-1];
            byte colorid = byte.Parse(mparts[2]);
            if(slot.slotStatus == (byte)SlotStatus.OCCUPIED){
                slot.color = colorid; //do this in game. also lol colors by id
                g.UpdateSlots();
                return new CommandResponse("Set Slot "+sid+" to COLOR "+colorid,false,false);
            }else{
                return new CommandResponse("That slot is not occupied",false,false);
            }
        }
    }

    public class ChangeHandicapCommand : ICommand {
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            var mparts = msg.Split(" ");
            if(mparts.Length != 3) return null;
            
            int sid = int.Parse(mparts[1]);
            Slot slot = g.slots[sid-1];
            byte handicap = byte.Parse(mparts[2]);
            if(slot.slotStatus == (byte)SlotStatus.OCCUPIED){
                slot.handicap = handicap; // do this properly in game class, also lol allowing arbitrary input for something that isnt arbitrary
                g.UpdateSlots();
                return new CommandResponse("Set Slot "+sid+" to HANDICAP "+handicap,false,false);
            }else{
                return new CommandResponse("That slot is not occupied",false,false);
            }
        }
    }

    public class ComputerCommand : ICommand {
        public CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg){
            var mparts = msg.Split(" ");
            if(mparts.Length != 2) return null;
            
            int sid = int.Parse(mparts[1]);
            Slot slot = g.slots[sid-1];
            if(slot.slotStatus == (byte)SlotStatus.OCCUPIED){
                return new CommandResponse("That slot is occupied already",false,false);
            }else{
                slot.color=g.GetUnusedColor();
                slot.slotStatus=(byte)SlotStatus.OCCUPIED;
                slot.pid=0;
                slot.downloadStatus=100;
                slot.computer=1;
                slot.computertype=(byte)SlotCompType.EASY;
                slot.race=(byte)SlotRace.RANDOM|(byte)SlotRace.SELECTABLE;
                g.UpdateSlots();
                return new CommandResponse("Set Slot "+sid+" to be COMPUTER ( EASY )",false,false);
            }
        }
    }
}