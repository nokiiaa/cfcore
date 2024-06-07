using cfcore.Preprocessor;

namespace cfcore
{
    public class Warning : Token
    {
        public Warning(string text, string filename, int line, int col)
            : base(text, filename, line, col) { }
    }
}
