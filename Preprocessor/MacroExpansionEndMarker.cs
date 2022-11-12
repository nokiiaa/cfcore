namespace cfcore.Preprocessor
{
    public class MacroExpansionEndMarker : Token
    {
        public MacroExpansionEndMarker(string filename = "", 
            int line = 0, int col = 0) 
            : base(null, filename, line, col)
        {
        }
    }
}
