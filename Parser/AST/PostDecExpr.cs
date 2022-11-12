namespace cfcore.Parser.AST
{
    public class PostDecExpr : Expr
    {
        public Expr Expr { get; set; }
        public PostDecExpr((int, int) pos, string filename, Expr expr)
            : base(pos, filename)
            => Expr = expr;
    }
}
