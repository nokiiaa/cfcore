using cfcore.Preprocessor;

namespace cfcore.Parser.AST
{
    public abstract class NumberLiteral { }

    public class IntLiteral : NumberLiteral
    {
        public ulong Value { get; set; }
        public bool Unsigned { get; set; }
        public bool Long { get; set; }
        public bool LongLong { get; set; }
        public IntLiteral(ulong value, bool unsigned = false, bool @long = false, bool longlong = false)
        {
            Value = value;
            Unsigned = unsigned;
            Long = @long;
            LongLong = longlong;
        }
    }

    public class FloatLiteral : NumberLiteral
    {
        public double Value { get; set; }
        public bool Float { get; set; }
        public bool LongDouble { get; set; }

        public FloatLiteral(double value, bool @float = false, bool longDouble = false)
        {
            Value = value;
            Float = @float;
            LongDouble = longDouble;
        }
    }

    public class LiteralExpr : Expr
    {
        public Token Value { get; set; }
        public NumberLiteral ParsedNumber { get; set; }
        public LiteralExpr((int, int) pos, string filename, Token value, NumberLiteral parsed = null)
            : base(pos, filename)
        {
            Value = value;
            ParsedNumber = parsed;
        }
    }
}
