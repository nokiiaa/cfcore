using cfcore.Preprocessor;

namespace cfcore.Parser.AST
{
    public class UnaryExpr : Expr
    {
        public OperatorType Op { get; set; }
        public Expr Expr { get; set; }
        public UnaryExpr((int, int) pos, string filename, OperatorType op, Expr expr)
            : base(pos, filename)
        {
            Op = op;
            Expr = expr;
        }
    }
}
