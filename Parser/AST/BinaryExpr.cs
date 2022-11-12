using cfcore.Preprocessor;

namespace cfcore.Parser.AST
{
    public class BinaryExpr : Expr
    {
        public OperatorType Op { get; set; }
        public Expr Left { get; set; }
        public Expr Right { get; set; }
        public BinaryExpr((int, int) pos, string filename, OperatorType op, Expr left, Expr right)
            : base(pos, filename)
        {
            Op = op;
            Left = left;
            Right = right;
        }
    }
}
