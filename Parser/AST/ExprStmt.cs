namespace cfcore.Parser.AST
{
    public class ExprStmt : Stmt
    {
        public Expr Expr { get; set; }
        public ExprStmt((int, int) pos, string filename, Expr expr)
            : base(pos, filename) => Expr = expr;
    }
}
