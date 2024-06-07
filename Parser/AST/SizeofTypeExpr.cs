namespace cfcore.Parser.AST
{
    public class SizeofTypeExpr : Expr
    {
        public CType Type { get; set; }
        public SizeofTypeExpr((int, int) pos, string filename, CType type) :
            base(pos, filename) => Type = type;
    }
}
