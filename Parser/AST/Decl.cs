using System.Collections.Generic;

namespace cfcore.Parser.AST
{
    public class Decl : Node
    {
        public bool InlineFunction { get; set; }
        public StorageClass StorageClass { get; set; }
        public List<(CType Type, string Name, Expr InitializerOrBitCount)> Declared { get; set; } 
            = new List<(CType, string, Expr)>();
        public Decl((int, int) pos, string filename, StorageClass @class = StorageClass.None, 
            List<(CType, string, Expr)> declared = null, bool inline = false) : base(pos, filename)
        {
            InlineFunction = inline;
            StorageClass = @class;
            if (declared != null)
                Declared = declared;
        }
    }
}
