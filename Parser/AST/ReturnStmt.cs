namespace cfcore.Parser.AST
{
    public class ReturnStmt : Stmt
    {
        public Expr Value { get; set; }

        public ReturnStmt((int, int) pos, string filename, Expr value)
            : base(pos, filename) => Value = value;
    }
}
