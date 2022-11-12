namespace cfcore.Preprocessor
{
    public class NewlineMarker : Token
    {
        public NewlineMarker(string filename = "", int line = 0, int col = 0) 
            : base("\n", filename, line, col) { }
    }
}
