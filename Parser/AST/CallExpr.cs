using System.Collections.Generic;

namespace cfcore.Parser.AST
{
    public class CallExpr : Expr
    {
        public Expr Callee { get; set; }
        public List<Expr> Arguments { get; set; } = new List<Expr>();
        public CallExpr((int, int) pos, string filename, Expr callee, List<Expr> args = null)
            : base(pos, filename)
        {
            Callee = callee;
            if (args != null) Arguments = args;
        }
    }
}
