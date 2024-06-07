namespace cfcore.Parser.AST
{
    public class SizeofExpr : Expr
    {
        public Expr Value { get; set; }

        public SizeofExpr((int, int) pos, string filename, Expr value)
            : base(pos, filename) => Value = value;
    }
}
