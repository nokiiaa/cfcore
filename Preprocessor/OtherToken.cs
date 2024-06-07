namespace cfcore.Preprocessor
{
    public class OtherToken : Token
    {
        public OtherToken(string value, string filename = "",
            int line = 0, int col = 0)
            : base(value, filename, line, col) { }
    }
}
