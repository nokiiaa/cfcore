using System.Collections.Generic;

namespace cfcore.Parser.AST
{
    // A declaration inside of a struct/union.
    public class StructUnionDecl : Decl
    {
        public StructUnionDecl((int, int) pos, string filename,
            List<(CType, string, Expr)> declared = null,
            bool inline = false)
            : base(pos, filename, StorageClass.None, declared, inline) { }
    }
}
