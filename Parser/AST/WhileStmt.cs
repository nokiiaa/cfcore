namespace cfcore.Parser.AST
{
    public class WhileStmt : Stmt
    {
        public Expr Condition { get; set; }
        public Stmt Code { get; set; }
        public WhileStmt((int, int) pos, string filename, Expr condition, Stmt code)
            : base(pos, filename)
        {
            Condition = condition;
            Code = code;
        }
    }
}
