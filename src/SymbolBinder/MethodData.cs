using System;
using System.Reflection;

namespace SymbolBinder
{
    [Serializable]
    public class MethodData
    {
        public int Token;
        public int SignatureToken;
        public string Name;

        public MethodData(MethodBase method)
        {
            Token = method.MetadataToken;

            MethodBody body = method.GetMethodBody();
            if (body != null)
                SignatureToken = body.LocalSignatureMetadataToken;

            Name = method.DeclaringType.FullName + "::" + method.Name;

        }
    }
}
