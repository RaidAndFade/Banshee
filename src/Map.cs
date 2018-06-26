using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;

using Nito.KitchenSink.CRC;

using Foole.Mpq;

namespace WC3_PROTOCOL
{
    public class Map
    {
        byte[] mapData;
        int mapSize;
        byte[] crc;

        public Map(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open)) {
                byte[] d = new byte[fs.Length];
                fs.Read(d, 0, d.Length);
                fs.Seek(0,SeekOrigin.Begin);

                crc = new CRC32().ComputeHash(d, 0, d.Length);
                Console.WriteLine(string.Join(',',crc));

                using (MpqArchive a = new MpqArchive(fs,true))
                {
                    string[] filenames = { "(listfile)", "war3map.j", "scripts\\war3map.j", "war3map.w3e", "war3map.wpm", "war3map.doo", "war3map.w3u", "war3map.w3b", "war3map.w3d", "war3map.w3a", "war3map.w3q" };
                    foreach (string fn in filenames)
                    {
                        try
                        {
                            a.AddFilename(fn);
                        }
                        catch (Exception e) { }
                    }


                    for (int i = 0; i < a.Count; i++)
                    {
                        Console.WriteLine(a[i]);
                    }
                }
            }
            Thread.Sleep(10000);
        }
    }
}