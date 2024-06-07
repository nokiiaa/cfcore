using cfcore.Preprocessor;

namespace cfcore.Parser.AST
{
    public class GroupExpr : Expr
    {
        public Expr Expr { get; set; }
        public GroupExpr((int, int) pos, string filename, Expr expr)
            : base(pos, filename) => Expr = expr;
    }
}
