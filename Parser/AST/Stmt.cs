namespace cfcore.Parser.AST
{
    public class Stmt : Node
    {
        public Stmt((int, int) pos, string filename) : base(pos, filename) { }
    }
}
