using System.IO;

namespace WC3_PROTOCOL.packets
{
    public interface IPacket
    {
        IPacket parse(BinaryReader br);   
        byte[] toBytes();
    }
}