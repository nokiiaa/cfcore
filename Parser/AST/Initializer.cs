using System.Collections.Generic;

namespace cfcore.Parser.AST
{
    public class Initializer : Expr
    {
        public Designator Designator { get; set; }
        public Initializer((int, int) pos, string filename, Designator des)
            : base(pos, filename) => Designator = des;
    }

    public class ExprInitializer : Initializer
    {
        public Expr Expr { get; set; }
        public ExprInitializer((int, int) pos, string filename, Designator des, Expr expr)
            : base(pos, filename, des) => Expr = expr;
    }

    public class ListInitializer : Initializer
    {
        public List<Initializer> Initializers { get; set; } 
            = new List<Initializer>();
        public ListInitializer((int, int) pos, string filename, Designator des, List<Initializer> initializers = null)
            : base(pos, filename, des)
        {
            if (initializers != null)
                Initializers = initializers;
        }
    }
}
