using Banshee.Ingame;

namespace Banshee.Commands{
    public interface ICommand{
        CommandResponse onTrigger(Game g, ConnectedPlayer p, string msg);
    }
}