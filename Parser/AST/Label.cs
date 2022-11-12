namespace cfcore.Parser.AST
{
    public abstract class Label { }

    public class CaseLabel : Label
    {
        public Expr Activation { get; set; }

        public CaseLabel(Expr activation)
            => Activation = activation;
    }
    public class DefaultLabel : Label
    {
        public DefaultLabel() { }
    }

    public class NamedLabel : Label
    {
        public string Name { get; set; }

        public NamedLabel(string name)
            => Name = name;
    }
}
