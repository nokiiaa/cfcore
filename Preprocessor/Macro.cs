using System.Collections.Generic;

namespace cfcore.Preprocessor
{
    public class Macro
    {
        public List<Token> Expansion { get; set; } = new List<Token>();

        public Macro(List<Token> expansion = null)
        {
            if (expansion != null) Expansion = expansion;
        }
    }

    public class FunctionMacro : Macro
    {
        public List<string> Arguments { get; set; }
            = new List<string>();

        public FunctionMacro(List<string> args, List<Token> expansion = null)
            : base(expansion)
        {
            if (args != null) Arguments = args;
        }
    }
}
