namespace cfcore.Parser.AST
{
    public class PtrExpr : Expr
    {
        public Expr Expr { get; set; }
        public string Member { get; set; }
        public PtrExpr((int, int) pos, string filename, Expr expr, string member)
            : base(pos, filename)
        {
            Expr = expr;
            Member = member;
        }
    }
}
