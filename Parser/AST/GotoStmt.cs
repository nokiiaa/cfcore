namespace cfcore.Parser.AST
{
    public class GotoStmt : Stmt
    {
        public string Name { get; set; }
        
        public GotoStmt((int, int) pos, string filename, string name)
            : base(pos, filename) => Name = name;
    }
}
