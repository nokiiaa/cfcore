namespace cfcore.Parser.AST
{
    public class IdentExpr : Expr
    {
        public string Name { get; set; }

        public IdentExpr((int, int) pos, string filename, string name)
            : base(pos, filename) => Name = name;
    }
}
