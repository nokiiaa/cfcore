namespace cfcore.Parser.AST
{
    public class LabeledStmt : Stmt
    {
        public Label Label { get; set; }
        public Stmt Statement { get; set; }

        public LabeledStmt((int, int) pos, string filename, Label label, Stmt stmt)
            : base(pos, filename)
        {
            Label = label;
            Statement = stmt;
        }
    }
}
