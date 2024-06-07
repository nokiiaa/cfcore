using System.Collections.Generic;

namespace cfcore.Parser.AST
{
    public class BlockStmt : Stmt
    {
        public List<Node> Items { get; set; } = new List<Node>();
        public BlockStmt((int, int) pos, string filename, List<Node> items = null)
            : base(pos, filename)
        {
            if (items != null) Items = items;
        }
    }
}
