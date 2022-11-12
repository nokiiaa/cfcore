using System.IO;

namespace cfcore.Preprocessor
{
    public class Token
    {
        public string Value { get; set; }
        public (int Line, int Column) Position { get; set; }
        public string Filename { get; set; }

        public Token(string value, string filename = "", int line = 0, int col = 0)
        {
            Position = (line, col);
            Filename = filename;
            Value = value;
        }

        public static implicit operator bool(Token t) => t != null;
    }
}
