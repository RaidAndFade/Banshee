using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Collections.Generic;

using Nito.KitchenSink.CRC;

using Foole.Mpq;

namespace Banshee
{
    public class Map
    {
        byte[] mapData;

        //shit figured out from mpq
        byte[] MapSize;
        byte[] MapInfo;
        byte[] MapCRC;
        byte[] MapSha;

        public Map(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open)) {
                byte[] d = new byte[fs.Length];
                fs.Read(d, 0, d.Length);
                fs.Seek(0,SeekOrigin.Begin);

                MapInfo = new CRC32().ComputeHash(d, 0, d.Length);

                using (MpqArchive a = new MpqArchive(fs,true))
                {
                    uint CRCVal = 0;
                    
                    SHA1Managed sha = new SHA1Managed();

                    byte[] commonJData, blizzardJData;
                    if(a.FileExists("Scripts\\common.j")){
                        using(var commonOverload = a.OpenFile("Scripts\\common.j")){
                            var ms = new MemoryStream();
                            commonOverload.CopyTo(ms);
                            commonJData = ms.ToArray();
                            ms.Dispose();
                        }
                    }else{
                        commonJData = File.ReadAllBytes("dep/common.j");
                    }
                    if(a.FileExists("Scripts\\blizzard.j")){
                        using(var blizzardOverload = a.OpenFile("Scripts\\blizzard.j")){
                            var ms = new MemoryStream();
                            blizzardOverload.CopyTo(ms);
                            blizzardJData = ms.ToArray();
                            ms.Dispose();
                        }
                    }else{
                        blizzardJData = File.ReadAllBytes("dep/blizzard.j");
                    }

                    CRCVal = CRCVal ^ XORRotateLeft(commonJData);
                    CRCVal = CRCVal ^ XORRotateLeft(blizzardJData);

                    sha.TransformBlock(commonJData,0,commonJData.Length,commonJData,0);
                    sha.TransformBlock(blizzardJData,0,blizzardJData.Length,blizzardJData,0);

                    var magicBytes = new byte[]{0x9e,0x37,0xf1,0x03};
                    uint magicInt = 0x03F1379E;

                    CRCVal = ROTL(CRCVal, 3);
                    CRCVal = ROTL(CRCVal ^ magicInt, 3);

                    sha.TransformBlock(magicBytes,0,magicBytes.Length,magicBytes,0);
                    
                    string[] filenames = { "war3map.j", "scripts\\war3map.j", "war3map.w3e", "war3map.wpm", "war3map.doo", "war3map.w3u", "war3map.w3b", "war3map.w3d", "war3map.w3a", "war3map.w3q" };
                    var foundScript = false;
                    
                    foreach (string fn in filenames)
                    {
                        if(foundScript && fn == filenames[2]) continue;

                        if(!a.FileExists(fn)) continue;

                        using(MpqStream s = a.OpenFile(fn)){
                            var ms = new MemoryStream();
                            s.CopyTo(ms);
                            var fdata = ms.ToArray();
                            ms.Dispose();

                            CRCVal = ROTL(CRCVal ^ XORRotateLeft(fdata),3);
                            sha.TransformBlock(fdata,0,fdata.Length,fdata,0);
                        }
                    }

                    MapCRC = BitConverter.GetBytes(CRCVal);

                    sha.TransformFinalBlock(new byte[]{},0,0);
                    MapSha = sha.Hash;
                }
            }


            System.Console.WriteLine(BitConverter.ToString(MapInfo).Replace("-",""));
            System.Console.WriteLine(BitConverter.ToString(MapCRC).Replace("-",""));
            System.Console.WriteLine(BitConverter.ToString(MapSha).Replace("-",""));
            Thread.Sleep(10000);
        }


        public uint ROTL(uint i,int c){
            return (i << c) | (i >> (32 - c));
        }
        public uint XORRotateLeft(byte[] d){
            uint i=0;
            uint v=0;
            int l=d.Length;

            if(l>3){
                while(i<l-3){
                    v = ROTL(v ^ (((uint)d[i]) + ((uint)d[i+1]<<8) + ((uint)d[i+2]<<16) + ((uint)d[i+3]<<24)), 3);
                    i+=4;
                }
            }

            while(i<l){
                v = ROTL(v ^ d[i], 3);
                i++;
            }

            return v;
        }
    }
}