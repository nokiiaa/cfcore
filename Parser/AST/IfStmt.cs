namespace cfcore.Parser.AST
{
    public class IfStmt : Stmt
    {
        public Expr Condition { get; set; }
        public Stmt If { get; set; }
        public Stmt Else { get; set; }
        public IfStmt((int, int) pos, string filename, Expr condition, 
            Stmt @if, Stmt @else = null)
            : base(pos, filename)
        {
            If = @if;
            Else = @else;
            Condition = condition;
        }
    }
}
