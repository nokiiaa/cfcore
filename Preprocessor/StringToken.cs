using cfcore.Parser.AST;
using System;
using System.Collections.Generic;

namespace cfcore.Preprocessor
{
    public class StringToken : Token
    {
        public bool Wide { get; set; }
        public bool Char { get; set; }
        public bool IncludeDirectory { get; set; }
        public StringToken(string value, bool wide = false, bool @char = false, bool inc = false,
            int line = 0, int col = 0, string filename = "") 
            : base(value, filename, line, col)
        {
            Wide = wide;
            IncludeDirectory = inc;
            Char = @char;
        }

        public IntLiteral CharToInteger(List<Warning> warnings, List<Error> errors, int size = 4)
        {
            void warn(string warning) =>
                warnings.Add(new Warning(warning, Filename, Position.Line, Position.Column));
            void error(string error) =>
                errors.Add(new Error(error, Filename, Position.Line, Position.Column));

            if (!Char)
            {
                error("Must be a character constant");
                return null;
            }
            else
            {
                int pos = 0;
                bool end() => pos >= Value.Length;
                ulong result = 0;
                int len = 0;
                while (!end())
                {
                    // Check if at second character of constant
                    if (len == 1)
                        warn("Multi-character character constant");
                    char c = '\0';
                    if (Value[pos] == '\\')
                    {
                        pos++;
                        if (!end())
                        {
                            int n = 0;
                            switch (Value[pos++])
                            {
                                case 'a': c = '\a'; break;
                                case 'b': c = '\b'; break;
                                case 'e': c = '\x1B'; break;
                                case 'f': c = '\f'; break;
                                case 'n': c = '\n'; break;
                                case 'r': c = '\r'; break;
                                case 't': c = '\t'; break;
                                case 'v': c = '\v'; break;
                                case '\'': c = '\''; break;
                                case '"': c = '"'; break;
                                case '?': c = '?'; break;
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                    pos--;
                                    int oct = 0;
                                    while (!end() && n <= 3 && Value[pos] >= '0' && Value[pos] <= '7')
                                    {
                                        oct <<= 3;
                                        oct |= Value[pos++] - '0';
                                        n++;
                                    }
                                    if (oct > 0xFF)
                                        warn("Octal escape sequence out of range");
                                    oct &= 0xFF;
                                    c = (char)oct;
                                    break;
                                case 'x':
                                case 'u':
                                    int maxc = Value[pos] == 'x' ? 2 : 4;
                                    int max = maxc == 2 ? 0xFF : 0xFFFF;
                                    int hex = 0;
                                    while (!end() && n <= maxc && ((Value[pos] >= '0' && Value[pos] <= '9') 
                                        || (char.ToLower(Value[pos]) >= 'a' &&
                                        char.ToLower(Value[pos]) <= 'f')))
                                    {
                                        hex <<= 4;
                                        char h = char.ToLower(Value[pos++]);
                                        hex |= h - (h > '9' ? 'a' - 10 : '0');
                                        n++;
                                    }
                                    if (hex > max)
                                        warn("Octal escape sequence out of range");
                                    hex &= max;
                                    c = (char)hex;
                                    break;
                                default:
                                    c = Value[pos - 1];
                                    break;
                            }
                        }
                    }
                    else
                        c = Value[pos++];
                    ulong old = result;
                    result <<= Wide ? 16 : 8;
                    result |= c;
                    if (size == 8 ? old > result : ((1ul << (size * 8)) - 1) < result)
                    {
                        warn($"Character constant does not fit into {size * 8} bits");
                        result = old;
                        break;
                    }
                    len++;
                }
                return new IntLiteral(result);
            }
        }
    }
}
