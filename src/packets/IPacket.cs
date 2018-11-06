using System.IO;

namespace Banshee.Packets
{
    public interface IPacket
    {
        IPacket parse(BinaryReader br, int len);   
        byte[] toBytes();
    }
}