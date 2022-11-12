namespace cfcore.Parser.AST
{
    public class ContinueStmt : Stmt
    {
        public ContinueStmt((int, int) pos, string filename)
            : base(pos, filename) { }
    }
}
