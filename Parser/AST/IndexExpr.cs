namespace cfcore.Parser.AST
{
    public class IndexExpr : Expr
    {
        public Expr Expr { get; set; }
        public Expr Index { get; set; }
        public IndexExpr((int, int) pos, string filename, Expr expr, Expr index)
            : base(pos, filename)
        {
            Expr = expr;
            Index = index;
        }
    }
}
