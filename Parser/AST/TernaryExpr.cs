using System;
using System.Collections.Generic;
using System.Text;

namespace cfcore.Parser.AST
{
    public class TernaryExpr : Expr
    {
        public Expr Condition { get; set; }
        public Expr If { get; set; }
        public Expr Else { get; set; }

        public TernaryExpr((int, int) pos, string filename, Expr condition, Expr @if, Expr @else)
            : base(pos,filename)
        {
            Condition = condition;
            If = @if;
            Else = @else;
        }
    }
}
