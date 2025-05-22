using System.Text;

namespace FPorter300.WMO
{
    internal class Program
    {
        [Flags]
        public enum MOPYFlags : ushort
        {
            UNK_0x1 = 0x1,
            NOCAMCOLLIDE = 0x2,
            DETAIL = 0x4,
            COLLISION = 0x8,
            HINT = 0x10,
            RENDER = 0x20,
            CULL_OBJECTS = 0x40,
            COLLIDE_HIT = 0x80,
            UNK_0x100 = 0x100,
            UNK_0x200 = 0x200,
            UNK_0x400 = 0x400,
            UNK_0x800 = 0x800,
            UNK_0x1000 = 0x1000,
            UNK_0x2000 = 0x2000,
            UNK_0x4000 = 0x4000,
            UNK_0x8000 = 0x8000,
        }

        static void Main(string[] args)
        {
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            var sourceDir = Path.Combine(exeDir, "input");
            var outputDir = Path.Combine(exeDir, "output");

            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine("Input directory not found. Please create an 'input' directory with WMO files in their correct directory structure (root and group WMOs) to patch.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            Console.WriteLine("Copying files from " + sourceDir + " to " + outputDir);
            foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
            {
                var targetDir = Path.GetDirectoryName(file)!.Replace(sourceDir, outputDir);
                Directory.CreateDirectory(targetDir);
                var targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile, true);
            }

            Console.WriteLine("Patching WMOs in " + outputDir);
            foreach (var file in Directory.GetFiles(outputDir, "*.wmo", SearchOption.AllDirectories))
            {
                var baseName = Path.GetFileName(file);
                using (var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite))
                using (var bin = new BinaryReader(fs))
                using (var bw = new BinaryWriter(fs))
                {
                    while (bin.BaseStream.Position < bin.BaseStream.Length)
                    {
                        var chunkNameBytes = bin.ReadBytes(4);
                        var chunkName = Encoding.ASCII.GetString(chunkNameBytes.Reverse().ToArray());
                        var chunkSize = bin.ReadUInt32();
                        var chunkStart = bin.BaseStream.Position;

                        if (chunkName == "MOMT")
                        {
                            for (int i = 0; i < chunkSize / 64; i++)
                            {
                                var startPos = bin.BaseStream.Position;

                                var flags = bin.ReadUInt32();
                                var shader = bin.ReadUInt32();
                                var blendMode = bin.ReadUInt32();
                                var texture1 = bin.ReadUInt32();
                                var color1 = bin.ReadUInt32();
                                var color1b = bin.ReadUInt32();
                                var texture2 = bin.ReadUInt32();
                                var color2 = bin.ReadUInt32();
                                var groundType = bin.ReadUInt32();
                                var texture3 = bin.ReadUInt32();
                                var color3 = bin.ReadUInt32();
                                var flags3 = bin.ReadUInt32();
                                var runtimeData0 = bin.ReadUInt32();
                                var runtimeData1 = bin.ReadUInt32();
                                var runtimeData2 = bin.ReadUInt32();
                                var runtimeData3 = bin.ReadUInt32();

                                if (shader == 23)
                                {
                                    Console.WriteLine("Patching shader " + file);
                                    bin.BaseStream.Position = startPos + 4;
                                    bw.Write((uint)13);

                                    if (color3 != 0)
                                    {
                                        bin.BaseStream.Position = startPos + 12;
                                        bw.Write(color3);
                                    }
                                    else
                                    {
                                        bin.BaseStream.Position = startPos + 12;
                                        bw.Write(texture2);
                                    }
                                }

                                bin.BaseStream.Position = startPos + 64;
                            }
                        }

                        if (chunkName == "MOGP")
                        {
                            bin.BaseStream.Position += 68; // header
                            while (bin.BaseStream.Position < chunkStart + chunkSize)
                            {
                                var subChunkName = Encoding.ASCII.GetString(bin.ReadBytes(4).Reverse().ToArray());
                                var subChunkSize = bin.ReadUInt32();
                                var subChunkStart = bin.BaseStream.Position;

                                if (subChunkName == "MPY2")
                                {
                                    Console.WriteLine("Patching MPY2 " + file);
                                    var entries = new List<(MOPYFlags flags, ushort matID)>();

                                    for (int i = 0; i < subChunkSize / 4; i++)
                                    {
                                        var flags = (MOPYFlags)bin.ReadUInt16();
                                        var matID = bin.ReadUInt16();
                                        entries.Add((flags, matID));
                                    }

                                    bin.BaseStream.Position = subChunkStart - 8;
                                    bw.Write("YPOM".ToCharArray());
                                    bw.Write(subChunkSize);
                                    foreach (var (flags, matID) in entries)
                                    {
                                        var flagCopy = flags;

                                        if (flags.HasFlag(MOPYFlags.UNK_0x100))
                                            flagCopy &= ~MOPYFlags.UNK_0x100;

                                        bw.Write((sbyte)flagCopy);

                                        if (matID == 0xFFFF)
                                            bw.Write((sbyte)-1);
                                        else
                                            bw.Write((byte)matID);
                                    }
                                }

                                bin.BaseStream.Position = subChunkStart + subChunkSize;
                            }
                        }

                        bin.BaseStream.Position = chunkStart + chunkSize;
                    }
                }
            }

            Console.WriteLine("Done. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
