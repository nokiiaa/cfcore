namespace cfcore.Parser.AST
{
    public class ForStmt : Stmt
    {
        public Decl InitialDecl { get; set; }
        public Expr InitialExpr { get; set; }
        public Expr Condition { get; set; }
        public Expr Advance { get; set; }
        public Stmt Code { get; set; }
        public ForStmt((int, int) pos, string filename, Expr initialExpr, Expr condition, Expr advance, Stmt code)
            : base(pos, filename)
        {
            InitialExpr = initialExpr;
            Condition = condition;
            Advance = advance;
            Code = code;
        }

        public ForStmt((int, int) pos, string filename, Decl initialDecl, Expr condition, Expr advance, Stmt code)
            : base(pos, filename)
        {
            InitialDecl = initialDecl;
            Condition = condition;
            Advance = advance;
            Code = code;
        }
    }
}
