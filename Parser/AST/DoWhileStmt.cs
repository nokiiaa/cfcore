namespace cfcore.Parser.AST
{
    public class DoWhileStmt : WhileStmt
    {
        public DoWhileStmt((int, int) pos, string filename, Expr condition, Stmt code)
            : base(pos, filename, condition, code) { }
    }
}
