using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


namespace Caesar
{
    class Program
    {
        // During normal operation, this class is completely ignored
        // The project can be temporarily switched from class library to console application to run as a standalone binary
        static void Main(string[] args)
        {
            Console.WriteLine("Caesar (running as console application)");
            RunLibraryTest();
            Console.WriteLine("Done, press any key to exit");
            Console.ReadKey();
        }

        static void RunLibraryTest() 
        {
            // debug: step through files to observe potential faults, missing bitflags etc.
            //List<string> paths = new List<string>();
            //string basePath = Environment.CurrentDirectory;
            //LoadFilePaths(basePath + @"\Data\cbf\", paths);
            ////LoadFilePaths(basePath + @"CBF VAN\", paths);
            //foreach (string file in paths)
            //{
            //    Console.WriteLine(file);
            //    CaesarContainer container = new CaesarContainer(File.ReadAllBytes(file));
            //    File.WriteAllText(file.Replace(".cbf", ".json"), CaesarContainer.SerializeContainer(container));
            //    //Console.ReadKey();
            //}
            CaesarContainer container = new CaesarContainer(File.ReadAllBytes(Path.Combine(Environment.CurrentDirectory, @"Data\cbf\CPC2.1.20.19.cbf")));
            int unknown_index = 0;
            string outName_base = container.CaesarCFFHeader.CaesarCTFHeader.CtfModuleName ?? "UNK_MOD" + (unknown_index++); 
            if (container.CaesarCFFHeader.DscTable != null)
            {
                var dscObjs = container.CaesarCFFHeader.DscTable.GetObjects();
                for (int i = 0; i < dscObjs.Count; i++)
                {
                    for(int j = 0; j < dscObjs[i].DscFile.Functions.Values.Count; j++)
                    {
                        var funcObj = dscObjs[i].DscFile.Functions.Values[j];
                        if (funcObj.InstructionDump.Length > 0)
                        {
                            string fullName = $"{outName_base}_DSC{i}_FUNC_{j}_{funcObj.Name}.txt";
                            File.WriteAllText(fullName, BitUtility.BytesToHex(funcObj.InstructionDump, true));
                        }
                    }
                }
            }
            File.WriteAllText(outName_base + ".json", CaesarContainer.SerializeContainer(container));
        }

        static void LoadFilePaths(string path, List<string> result)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                if (Path.GetExtension(file).ToLower() == ".cbf")
                {
                    result.Add(file);
                }
            }
        }
    }
}
