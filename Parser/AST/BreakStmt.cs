namespace cfcore.Parser.AST
{
    public class BreakStmt : Stmt
    {
        public BreakStmt((int, int) pos, string filename)
            : base(pos, filename) { }
    }
}
