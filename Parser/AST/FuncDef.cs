namespace cfcore.Parser.AST
{
    public class FuncDef : Node
    {
        public bool Inline { get; set; }
        public CFunctionType Type { get; set; }
        public string Name { get; set; }
        public BlockStmt Code { get; set; }
        public FuncDef((int, int) pos, string filename, CFunctionType type, string name, BlockStmt code,
            bool inline = false)
            : base(pos, filename)
        {
            Inline = inline;
            Type = type;
            Name = name;
            Code = code;
        }
    }
}
