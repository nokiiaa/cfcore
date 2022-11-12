using System;

namespace cfcore.Preprocessor
{
    public class OperatorToken : Token
    {
        public OperatorType Type { get; private set; }
        public OperatorToken(string value, string filename = "",
            int line = 0, int col = 0) : base(value, filename, line, col)
            => Type = (OperatorType)Array.IndexOf(OperatorStrings.List, value);
    }
}
