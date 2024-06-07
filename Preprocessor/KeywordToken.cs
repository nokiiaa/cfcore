using System;

namespace cfcore.Preprocessor
{
    public class KeywordToken : Token
    {
        public KeywordType Type { get; private set; }
        public KeywordToken(string value, string filename = "",
            int line = 0, int col = 0)
            : base(value, filename, line, col)
            => Type = (KeywordType)Array.IndexOf(KeywordStrings.List, value);
    }
}
