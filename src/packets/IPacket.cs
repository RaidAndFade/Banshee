using System.IO;

namespace Banshee.packets
{
    public interface IPacket
    {
        IPacket parse(BinaryReader br);   
        byte[] toBytes();
    }
}