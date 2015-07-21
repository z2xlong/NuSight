using System;
using System.Collections.Generic;
using System.IO;

namespace SymbolBinder
{
    /// <summary>
    /// The installed Nuget Symbol Package in DEV environment
    /// </summary>
    public class NsPackage
    {
        private string _srcPath;
        private HashSet<string> _sources;
        private string[] _dlls;

        public IEnumerable<string> Assemblies
        {
            get
            {
                return _dlls;
            }
        }

        public bool HasSrc
        {
            get
            {
                return _sources != null && _sources.Count > 0;
            }
        }

        public NsPackage(string dir)
        {
            if (Directory.Exists(dir))
                Init(dir);
        }

        public bool TryMappingSourceFile(string path, out string src)
        {
            src = string.Empty;

            if (string.IsNullOrWhiteSpace(path))
                return false;
            if (this.HasSrc == false)
                return false;

            string relative = null;

            for (int i = path.Length - 1; i >= 0; i--)
            {
                if (path[i] == '\\')
                {
                    string sub = path.Substring(i);
                    if (_sources.Contains(sub))
                        relative = sub;
                    else if (relative != null)
                    {
                        src = _srcPath + relative;
                        return true;
                    }
                }
            }
            return false;
        }

        private void Init(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
                return;

            string src = Path.Combine(dir, "src");
            if (Directory.Exists(src))
            {
                _srcPath = src;
                _sources = new HashSet<string>();
                foreach (string file in Directory.GetFiles(src, "*.*", SearchOption.AllDirectories))
                    _sources.Add(file.Substring(this._srcPath.Length));
            }

            string lib = Path.Combine(dir, "lib");
            if(Directory.Exists(lib))
            {
                _dlls = Directory.GetFiles(lib, "*.dll", SearchOption.AllDirectories);
            }
        }

    }
}
