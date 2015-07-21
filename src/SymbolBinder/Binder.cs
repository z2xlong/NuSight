using Microsoft.Samples.Debugging.CorSymbolStore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SymbolBinder
{
    public class Binder
    {
        public static bool Execute(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            NsPackage pck = new NsPackage(path);
            if (pck.Assemblies == null || pck.HasSrc == false)
                return false;

            foreach (string dll in pck.Assemblies)
            {
                SymbolData symData = null;
                //todo: catch exception
                using (SymbolDataReader reader = new SymbolDataReader(dll, SymbolFormat.PDB, true))
                {
                    symData = reader.ReadSymbols();

                    if (symData == null || symData.sourceFiles == null || symData.sourceFiles.Count <= 0)
                        continue;

                    foreach (Document doc in symData.sourceFiles)
                    {
                        string src;
                        if (pck.TryMappingSourceFile(doc.url, out src))
                            doc.url = src;
                    }
                }
                //// Emit PDB
                SymbolDataWriter writer = new SymbolDataWriter(dll, SymbolFormat.PDB);
                writer.WritePdb(symData);
            }
            return true;

        }

        private static int CommonRootPath(string path1, string path2)
        {
            if (string.IsNullOrWhiteSpace(path1) || string.IsNullOrWhiteSpace(path2))
                return 0;

            int latestSlash = 0;
            int len = Math.Min(path1.Length, path2.Length);

            for (int i = 0; i < len; i++)
            {
                if (path1[i] != path2[i])
                    break;
                else if (path1[i] == '\\')
                    latestSlash = i;
            }

            return latestSlash;
        }

    }
}
