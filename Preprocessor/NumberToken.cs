using cfcore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace cfcore.Preprocessor
{
    public class NumberToken : Token
    {
        public NumberToken(string value, string filename = "",
            int line = 0, int col = 0)
            : base(value, filename, line, col) { }

        public NumberLiteral Parse(bool allowFloat = false, List<Warning> warnings = null, List<Error> errors = null)
        {
            int pos = 0;
            NumberLiteral nl = null;
            void warn(string warning)
            {
                if (warnings != null)
                    warnings.Add(new Warning(warning, Filename, Position.Line, Position.Column));
            }

            void error(string error)
            {
                if (errors != null)
                    errors.Add(new Error(error, Filename, Position.Line, Position.Column));
            }

            char at(int index) => index < 0 || index >= Value.Length ? '\0' : Value[index];
            char at_rel(int offset) => at(pos + offset);
            bool end() => pos >= Value.Length;
            char current() => at(pos);
            char next() => at_rel(+1);
            char advance(int n = 1) { char c = at(pos); pos += n; return c; }
            bool match(char a, char b = '\0', char c = '\0', char d = '\0') =>
                current() == a && (b == '\0' || next() == b) && (c == '\0' || at_rel(2) == c)
                && (d == '\0' || at_rel(3) == d) ?
                (advance(b == '\0' ? 1 : c == '\0' ? 2 : d == '\0' ? 3 : 4) != '\0') : false;
            bool check(char a) => current() == a;

            // Matches a string of characters satisfying a certain requirement.
            string s_match(Predicate<char> filter)
            {
                int p = pos;
                while (!end() && filter(current())) pos++;
                return Value.Substring(p, pos - p);
            }

            double exponent()
            {
                bool sign = true;
                if (match('-')) sign = false;
                else match('+');
                string sExp = s_match(x => x >= '0' && x <= '9');
                if (sExp == "")
                {
                    error("Expected one or more decimal digits");
                    return 0;
                }
                return double.Parse(sExp) * (sign ? 1 : -1);
            }

            (bool f, bool l) float_suffix()
            {
                     if (match('f') || match('F')) return (true, false);
                else if (match('l') || match('L')) return (true, true);
                return (false, false);
            }

            (bool u, bool l, bool ll) int_suffix()
            {
                if (!check('u') && !check('U') && !check('l') && !check('L'))
                    return (false, false, false);
                if (match('u', 'l', 'l')) return (true, false, true);
                if (match('U', 'L', 'L')) return (true, false, true);
                if (match('U', 'l', 'l')) return (true, false, true);
                if (match('u', 'L', 'L')) return (true, false, true);
                if (match('l', 'l', 'u')) return (true, false, true);
                if (match('L', 'L', 'U')) return (true, false, true);
                if (match('l', 'l', 'U')) return (true, false, true);
                if (match('L', 'L', 'u')) return (true, false, true);
                if (match('u', 'l')) return (true, true, false);
                if (match('u', 'L')) return (true, true, false);
                if (match('U', 'l')) return (true, true, false);
                if (match('U', 'L')) return (true, true, false);
                if (match('l', 'u')) return (true, true, false);
                if (match('L', 'U')) return (true, true, false);
                if (match('L', 'u')) return (true, true, false);
                if (match('l', 'U')) return (true, true, false);
                if (match('l', 'l')) return (false, false, true);
                if (match('L', 'L')) return (false, false, true);
                if (match('l')) return (false, true, false);
                if (match('L')) return (false, true, false);
                if (match('u')) return (true, false, false);
                if (match('U')) return (true, false, false);
                return (false, false, false);
            }

            bool is_hex(char c) => c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';
            bool is_dec(char c) => c >= '0' && c <= '9';
            bool is_oct(char c) => c >= '0' && c <= '7';

            if (match('0', 'x'))
            {
                string hex = s_match(is_hex);
                string hexAfterDot = "";
                double exp = 0;
                bool floating = false;
                if (match('.'))
                {
                    floating = true;
                    hexAfterDot = s_match(is_hex);
                }
                if (match('p') || match('P'))
                {
                    floating = true;
                    exp = exponent();
                }
                else if (floating)
                    error("Hex floating-point constant needs exponent");
                if (!floating)
                {
                    if (hex == "")
                        error("Expected one or more hex digits");
                    hex = hex.TrimStart('0');
                    if (hex.Length > 16)
                    {
                        warn("Hex literal does not fit into 64 bits");
                        hex = hex.Substring(hex.Length - 16);
                    }
                    ulong res = ulong.Parse("0" + hex, NumberStyles.HexNumber);
                    var i = int_suffix();
                    nl = new IntLiteral(res, i.u, i.l, i.ll);
                }
                else
                {
                    if (hex == "" && hexAfterDot == "")
                        error("Expected one or more hex digits before/after '.'");
                    var fs = float_suffix();
                    double result = 0;
                    // Manually converting hex mantissa to a double, because
                    // apparently double.Parse can't...
                    string mantissa = hex.TrimStart('0') + hexAfterDot.TrimEnd('0');
                    // double gives 13 hex digits of precision
                    int min = Math.Min(13, mantissa.Length);
                    for (int i = 0; min > i; i++)
                    {
                        result *= 16;
                        result += (mantissa[i] - (mantissa[i] > '9' ? ('a' - 10) : '0'));
                    }
                    result *= Math.Pow(16, -hexAfterDot.Length);
                    result *= Math.Pow(2, exp);
                    nl = new FloatLiteral(result, fs.f, fs.l);
                }
            }
            else if (current() >= '0' && current() <= '9' || current() == '.')
            {
                if (Value[0] == '0' && !Value.Contains('e') && !Value.Contains('.'))
                {
                    var oct = s_match(is_oct).TrimStart('0');
                    ulong octRes = 0;
                    int bits = 0;
                    for (int i = 0; oct.Length > i; i++)
                    {
                        if (bits >= 63 && oct[0] > '1')
                        {
                            warn("Octal literal does not fit into 64 bits");
                            break;
                        }
                        octRes <<= 3;
                        octRes |= (ulong)oct[i] & 7;
                        bits += 3;
                    }
                    var @is = int_suffix();
                    nl = new IntLiteral(octRes, @is.u, @is.l, @is.ll);
                }
                else
                {
                    string dec = s_match(is_dec);
                    string decAfterDot = "";
                    double exp = 0;
                    bool floating = false;
                    if (match('.'))
                    {
                        floating = true;
                        decAfterDot = s_match(is_dec);
                    }
                    if (match('e') || match('E'))
                    {
                        floating = true;
                        exp = exponent();
                    }
                    if (floating)
                    {
                        if (dec == "" && decAfterDot == "")
                            error("Expected one or more decimal digits before/after '.'");
                        dec = "0" + dec.TrimStart('0');
                        decAfterDot = decAfterDot.TrimEnd('0') + "0";
                        double res = double.Parse($"{dec}.{decAfterDot}");
                        res *= Math.Pow(10, exp);
                        var fs = float_suffix();
                        nl = new FloatLiteral(res, fs.f, fs.l);
                    }
                    else
                    {
                        ulong decRes = 0;
                        for (int i = 0; dec.Length > i; i++)
                        {
                            ulong old = decRes;
                            decRes *= 10;
                            decRes += (ulong)dec[i] - '0';
                            if (old > decRes)
                            {
                                warn("Decimal literal does not fit into 64 bits");
                                decRes = old;
                                break;
                            }
                        }
                        var @is = int_suffix();
                        nl = new IntLiteral(decRes, @is.u, @is.l, @is.ll);
                    }
                }
            }
            if (!end())
                error($"Unknown suffix {Value.Substring(pos)}");
            return nl;
        }
    }
}
