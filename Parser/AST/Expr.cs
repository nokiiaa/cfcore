namespace cfcore.Parser.AST
{
    public class Expr : Node
    {
        public Expr((int, int) pos, string filename) : base(pos, filename) { }
    }
}
