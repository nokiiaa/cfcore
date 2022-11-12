namespace cfcore.Parser.AST
{
    public class PostIncExpr : Expr
    {
        public Expr Expr { get; set; }
        public PostIncExpr((int, int) pos, string filename, Expr expr)
            : base(pos, filename)
            => Expr = expr;
    }
}
