namespace cfcore.Parser.AST
{
    public class DotExpr : Expr
    {
        public Expr Expr { get; set; }
        public string Member { get; set; }
        public DotExpr((int, int) pos, string filename, Expr expr, string member)
            : base(pos, filename)
        {
            Expr = expr;
            Member = member;
        }
    }
}
