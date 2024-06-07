using cfcore.Preprocessor;

namespace cfcore
{
    public class Error : Token
    {
        public Error(string text, string filename, int line, int col)
             : base(text, filename, line, col) { }
    }
}
