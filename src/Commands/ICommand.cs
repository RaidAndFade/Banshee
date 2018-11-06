using Banshee.Ingame;

namespace Banshee.Commands{
    public interface ICommand{
        CommandResponse? OnCall(Game g, ConnectedPlayer p, string msg);
    }
}