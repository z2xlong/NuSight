using System;
using System.Collections.Generic;

namespace SymbolBinder
{
    /// <summary>
    /// Represents all the data we want to read out of an ISymbolReader
    /// </summary>
    public class SymbolData
    {
        /// <summary>
        /// Path of the assembly for which these symbols correspond.  
        /// For informational purposes only - not written to the symbol store
        /// </summary>
        public string assembly;

        /// <summary>
        /// MethodDef token (in hex) of the user entrypoint method, or null if none (eg. a DLL)
        /// </summary>
        public string entryPointToken;

        public List<Document> sourceFiles;

        public List<Method> methods;
    }

    /// <summary>
    /// Data from an ISymbolDocument
    /// </summary>
    public class Document
    {
        public int id;

        public string url;

        public Guid language;

        public Guid languageVendor;

        public Guid documentType;
    }

    /// <summary>
    /// Data from an ISymbolMethod
    /// </summary>
    public class Method
    {
        /// <summary>
        /// The type and name of this method.
        /// For informational purposes only - not written to the symbol store
        /// </summary>
        public string name;

        /// <summary>
        /// MethodDef token (in hex) of the method
        /// </summary>
        public string token;

        /// <summary>
        /// Signature token (in hex) of the local variable signature for this method (or null if no locals)
        /// This can't be read from the symbol store directly (it's read by reflection), but can be used when writing locals.
        /// </summary>
        public string localSigMetadataToken;

        /// <summary>
        /// This is set to true by the reader when it can't read the method body from the method because the CLR
        /// claims it is invalid.  There is at least one known C# compiler bug that can cause this.  The
        /// reader will not be able to provide a localSigMetadataToken in this case, and so the writer may not
        /// be able to write out the locals.
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool hasInvalidMethodBody;

        public List<SequencePoint> sequencePoints;

        public Scope rootScope;

        public List<SymAttribute> symAttributes;

        /// <summary>
        /// Optionally, C# custom debug information
        /// </summary>
        public CSharpCDI csharpCDI;
    }

    /// <summary>
    /// Data from ISymbolMethod.GetSequencePoints
    /// </summary>
    public class SequencePoint
    {
        public int ilOffset;

        public int sourceId;

        /// <summary>
        /// If true, this sequence point corresponds to a hidden (0xfeefee) sequence point.
        /// This property isn't directly read or written from the symbols, but inferred by the line number value.
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool hidden;

        public int startRow;

        public int startColumn;

        public int endRow;

        public int endColumn;
    }

    /// <summary>
    /// Data from an ISymbolScope
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// True if this scope is created implicity by the symbol library and so should not explicitly
        /// written out.
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool isImplicit;

        public int startOffset;

        public int endOffset;

        public List<Variable> locals;

        public List<Constant> constants;

        public List<Scope> scopes;

        public List<Namespace> usedNamespaces;

        /// <summary>
        /// True if this scope didn't actually show up while reading a PDB, but we know it must have
        /// been written there.  See SymbolDataReader.WorkAroundDiasymreaderScopeBug for details.
        /// </summary>
        [System.ComponentModel.DefaultValue(false)]
        public bool isReconstructedDueToDiasymreaderBug;
    }

    /// <summary>
    /// Data from an ISymbolVariable
    /// </summary>
    public class Variable
    {
        public string name;

        public int ilIndex;

        public int attributes;

        public string signature;
    }

    /// <summary>
    /// Data from an ISymbolConstant
    /// </summary>
    public class Constant
    {
        public string name;

        public string value;

        public string signature;
    }

    /// <summary>
    /// Data from an ISymbolNamespace
    /// Note that most uses of namespaces in the symbol APIs aren't actually implemented.
    /// Namespaces are just a name (no children or variables) and are used only in scopes to 
    /// say which namespaces are being "used".
    /// </summary>
    public class Namespace
    {
        public string name;
    }

    /// <summary>
    /// Data returned by ISymUnmanagedReader::GetSymAttribute
    /// </summary>
    public class SymAttribute
    {
        public string name;

        public string value;   // hex bytes
    }

    //
    // The following are used to represent C# custom debug information
    // Theese structures are undocumented implementation details of the C#
    // compiler and debugger interaction.
    //
    public class CSharpCDI
    {
        public int version;

        public CDIItem[] entries;
    }

    public abstract class CDIItem
    {
        public int version;
    }

    public class CDIUsing : CDIItem
    {
        /// <summary>
        /// This appears to be used as follows:
        /// There is one entry for each level of declaration nesting for the containing method.
        /// The value indicates the number of using-namespace declarations (from the PDB scope using
        /// data) that should be imported, and applied to the indicated nesting level.
        /// Presumably this impacts what symbols are available when doing expression evaluation in the
        /// context of a method.
        /// </summary>
        public int[] countOfUsing;
    }

    public class CDIForward : CDIItem
    {
        /// <summary>
        /// This data indicates that CDIUsing information should be imported from the specified
        /// method.  Presumably this is an optimizaton to avoid duplicating all the using namespace
        /// names in each method.
        /// </summary>
        public string tokenToForwardTo;
    }

    public class CDIForwardModule : CDIItem
    {
        /// <summary>
        /// This is similar to tokenToForwardTo, but appears to be treated slightly differently somehow.
        /// </summary>
        public string tokenOfModuleInfo;
    }

    public class CDIIteratorLocalBucket
    {
        public int ilOffsetStart;

        public int ilOffsetEnd;
    }

    public class CDIIteratorLocals : CDIItem
    {
        /// <summary>
        /// This causes local variables in iterator methods (which are actually implemented 
        /// as fields on the generated class) to be visible in the debugger.
        /// </summary>
        public CDIIteratorLocalBucket[] buckets;
    }

    public class CDIForwardIterator : CDIItem
    {
        /// <summary>
        /// This indicates that the current method is an iterator method and is implemented
        /// by the specified generated class.  This causes callstacks to display the name of 
        /// this method when they're actually inside a method on the generated iterator class.
        /// </summary>
        public string iteratorClassName;
    }

    public class CDIUnknown : CDIItem
    {
        public int kind;

        public string bytes;
    }
}
