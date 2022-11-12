namespace cfcore.Parser.AST
{
    public class Designator
    {
        public Designator Parent { get; set; }
        public Designator(Designator parent)
            => Parent = parent;
    }

    public class ExprDesignator : Designator
    {
        public Expr Expr { get; set; }
        public ExprDesignator(Designator parent, Expr expr)
            : base(parent) => Expr = expr;
    }

    public class MemberDesignator : Designator
    {
        public string Name { get; set; }
        public MemberDesignator(Designator parent, string name)
            : base(parent) => Name = name;
    }
}
