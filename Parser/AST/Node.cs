namespace cfcore.Parser.AST
{
    public class Node
    {
        public (int Line, int Column) Position { get; set; }
        public string Filename { get; set; }

        public Node((int, int) pos, string filename)
        {
            Position = pos;
            Filename = filename;
        }
    }
}
