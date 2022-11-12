namespace cfcore.Parser.AST
{
    public class SwitchStmt : Stmt
    {
        public Expr Condition { get; set; }
        public Stmt Code { get; set; }
        public SwitchStmt((int, int) pos, string filename, Expr condition, 
            Stmt code)
            : base(pos, filename)
        {
            Condition = condition;
            Code = code;
        }
    }
}
