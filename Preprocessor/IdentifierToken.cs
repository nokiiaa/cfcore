namespace cfcore.Preprocessor
{
    public class IdentifierToken : Token
    {
        public IdentifierToken(string value, string filename = "",
            int line = 0, int col = 0)
            : base(value, filename, line, col) { }
    }
}
