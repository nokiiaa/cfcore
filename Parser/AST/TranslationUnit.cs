using System.Collections.Generic;

namespace cfcore.Parser.AST
{
    public class TranslationUnit : Node
    {
        public List<Node> Nodes { get; set; } = new List<Node>();

        public TranslationUnit(List<Node> nodes = null)
            : base((1, 1), "")
        {
            if (nodes != null) Nodes = nodes;
        }
    }
}
