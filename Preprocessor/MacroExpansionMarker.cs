namespace cfcore.Preprocessor
{
    public class MacroExpansionMarker : Token
    {
        public MacroExpansionMarker(string value, string filename = "", 
            int line = 0, int col = 0) 
            : base(value, filename, line, col)
        {
        }
    }
}
