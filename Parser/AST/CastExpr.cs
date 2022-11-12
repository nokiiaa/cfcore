namespace cfcore.Parser.AST
{
    public class CastExpr : Expr
    {
        public Expr Expr { get; set; }
        public CType Type { get; set; }
        public CastExpr((int, int) pos, string filename, Expr expr, CType type)
            : base(pos, filename)
        {
            Expr = expr;
            Type = type;
        }
    }
}
