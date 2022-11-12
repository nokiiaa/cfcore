using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace cfcore.Preprocessor
{
    public partial class Preprocess
    {
        public static Dictionary<string, List<Token>> Cache { get; set; }
            = new Dictionary<string, List<Token>>();
        public string IdentifierDigits { get; set; }
            = "0123456789";
        public string IdentifierLetters { get; set; }
            = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_";
        public Standard Standard { get; set; }
        public List<Warning> Warnings { get; set; }
            = new List<Warning>();
        public List<Error> Errors { get; set; }
            = new List<Error>();
        public List<Token> Output { get; set; }
        public bool Trigraphs { get; set; }
        public string Code { get; set; }


        public string FilePath { get; set; }
        public string Filename => Path.GetFileName(FilePath);

        int _chindex = 0, _line = 1, _col = 1;
        bool _includeDirective = false;
        bool _crlf = false;

        char At(int index) => index >= Code.Length ? '\0' : Code[index];
        char AtRel(int offset) => At(_chindex + offset);

        char Previous() => AtRel(-1);
        char Current() => AtRel(0);
        char Next() => AtRel(1);

        char Check(string charArray)
        {
            foreach (char c in charArray) if (Check(c)) return c;
            return '\0';
        }
        bool Check(char a, char b, char c, char d)
            => AtRel(0) == a && AtRel(1) == b && AtRel(2) == c && AtRel(3) == d;
        bool Check(char a, char b, char c) => AtRel(0) == a && AtRel(1) == b && AtRel(2) == c;
        bool Check(char a, char b) => AtRel(0) == a && AtRel(1) == b;
        bool Check(char a) => AtRel(0) == a;
        void Error(string msg, int line = -1, int col = -1)
        {
            if (line == -1) line = _line;
            if (col == -1) col = _col;
            Errors.Add(new Error(msg, _filename ?? Filename, line, col));
        }

        void Warning(string msg, int line = -1, int col = -1)
        {
            if (line == -1) line = _line;
            if (col == -1) col = _col;
            Warnings.Add(new Warning(msg, _filename ?? Filename, line, col));
        }
        void Advance(int n)
        {
            for (int i = 0; n > i; i++)
                Advance();
        }

        char Advance()
        {
            if (End())
                return '\0';
            else
            {
                char c = Code[_chindex++];
                if (c == '\n')
                {
                    _line++;
                    _col = 1;
                }
                else
                    _col++;
                return c;
            }
        }

        char CheckAdvance(string charArray)
        {
            if (Check(charArray) != '\0')
                return Advance();
            return '\0';
        }

        bool CheckAdvance(char a, char b, char c, char d)
        {
            if (Check(a, b, c, d))
            {
                Advance(4);
                return true;
            }
            return false;
        }
        bool CheckAdvance(char a, char b, char c)
        {
            if (Check(a, b, c))
            {
                Advance(3);
                return true;
            }
            return false;
        }
        bool CheckAdvance(char a, char b)
        {
            if (Check(a, b))
            {
                Advance(2);
                return true;
            }
            return false;
        }
        bool CheckAdvance(char a)
        {
            if (Check(a))
            {
                Advance();
                return true;
            }
            return false;
        }
        bool Whitespace(char c) => c == ' ' || c == '\t' || c == '\v' || c == '\n'
                                || c == '\r' || c == '\0';

        bool End() => _chindex >= Code.Length;
        bool End(int i) => i >= Code.Length;

        public Preprocess(string file, List<string> incDirs = null, 
            Dictionary<string, Macro> macros = null, bool trigraphs = false,
            Standard standard = Standard.C99)
        {
            if (incDirs != null) IncludeDirectories = incDirs;
            if (macros != null) Macros = macros;
            FilePath = file;
            SetupPredefinedMacros();
            if ((Standard = standard) == Standard.GNU)
                IdentifierLetters += '$';
            Trigraphs = trigraphs;
            if (!Cache.ContainsKey(FilePath))
                Code = File.ReadAllText(file);
            _crlf = Code.Contains("\r\n");
        }

        public List<Token> Do(bool included = false)
        {
            _includeDirective = false;
            _chindex = 0;
            _tokindex = 0;
            if (Cache.ContainsKey(FilePath))
                Output = Cache[FilePath];
            else
            {
                Output = new List<Token>();
                if (Trigraphs) ReplaceTrigraphs();
                FixNewlines();
                ReplaceBackslashes();
                Tokenize();
            }
            if (!included)
            {
                try
                {
                    ProcessDirectives();
                }
                catch (StopException) { }
                PrepareTokensForParser();
            }
            return Output;
        }

        void FixNewlines()
        {
            if (_crlf)
                Code = Code.Replace("\r\n", "\n");
        }

        void ReplaceBackslashes()
        {
            var newLines = new List<string>();
            var oldLines = Code.Split('\n').ToList();
            for (int i = 0; oldLines.Count > i; i++)
            {
                int blankLines = 0;
                string l = oldLines[i];
                while (true)
                {
                    string trimmed =
                        l.TrimEnd('\0', '\r', '\v', '\t', ' ');
                    if (trimmed.Length > 0 && trimmed.Last() == '\\')
                    {
                        if (i == oldLines.Count)
                            Warning("Backslash found at EOF", i + 1, 1);
                        else
                        {
                            if (trimmed != l)
                                Warning("Additional whitespace after backslash", i + 1, 1);
                            l = trimmed.Substring(0, trimmed.Length - 1)
                                + oldLines[i + 1];
                            oldLines.RemoveAt(i + 1);
                            blankLines++;
                        }
                    }
                    else
                        break;
                }
                newLines.Add(l);
                for (int j = 0; blankLines > j; j++)
                    newLines.Add("");
            }
            Code = string.Join("\n", newLines);
        }

        void ReplaceTrigraphs()
        {
            Code = Code
                    .Replace("??=", "#")
                    .Replace("??/", @"\")
                    .Replace("??'", "^")
                    .Replace("??(", "[")
                    .Replace("??)", "]")
                    .Replace("??!", "|")
                    .Replace("??<", "{")
                    .Replace("??>", "}")
                    .Replace("??-", "~");
        }

        void AddToken(Token t, int l = -1, int c = -1)
        {
            if (l == -1 || c == -1)
                t.Position = (_line, _col);
            else
                t.Position = (l, c);
            t.Filename = Filename;
            Output.Add(t);
        }

        void Tokenize()
        {
            while (!End())
            {
                if (Output.Count >= 2 && Output.Last() is IdentifierToken it && it.Value == "include"
                    && Output[Output.Count - 2] is OperatorToken pt && 
                    (pt.Type == OperatorType.Hash || pt.Type == OperatorType.DiHash))
                    _includeDirective = true;
                int l = _line, c = _col;
                int start = _chindex;
                if (CheckAdvance('/', '/'))
                {
                    while (!End() && !CheckAdvance('\n'))
                        Advance();
                    continue;
                }
                else if (CheckAdvance('/', '*'))
                {
                    while (!CheckAdvance('*', '/'))
                    {
                        if (End())
                        {
                            Error("Unterminated comment");
                            break;
                        }
                        Advance();
                    }
                    continue;
                }
                else if (Current() >= '0' && Current() <= '9' || (Current() == '.'
                    && Next() >= '0' && Next() <= '9'))
                {
                    for (;;)
                    {
                        if (Current() == '.' || Current() >= '0' && Current() <= '9')
                            Advance();
                        else if (CheckAdvance(IdentifierLetters) != '\0')
                        {
                            if (Previous() == 'e' || Previous() == 'E' || Previous() == 'p' || Previous() == 'P')
                            {
                                if (!CheckAdvance('-'))
                                    CheckAdvance('+');
                            }
                        }
                        else
                            break;
                    }
                }
                int len = _chindex - start;
                if (len > 0)
                    AddToken(new NumberToken(Code.Substring(start, len)), l, c);
                else
                {
                    if (Check('\'') || Check('"') || (_includeDirective && Check('<')) ||
                        (Check('L') && (Next() == '\'' || Next() == '"')))
                    {
                        bool wide = false, chr = false, inc = false;
                        char term;
                        wide = CheckAdvance('L');
                        term = Current();
                        if (term == '<') { inc = true; term = '>'; }
                        chr = CheckAdvance('\'');
                        if (!chr && !CheckAdvance('"') && inc)
                            CheckAdvance('<');
                        start = _chindex;
                        bool invalid = false;
                        while (!CheckAdvance(term))
                        {
                            if (End() || Check('\n'))
                            {
                                Warning("Unterminated string constant");
                                Advance();
                                invalid = true;
                                break;
                            }
                            if (!_includeDirective)
                                CheckAdvance('\\');
                            Advance();
                        }
                        string final = Code.Substring(start, _chindex - start - 1);
                        if (_includeDirective) final = final.TrimEnd(' ');
                        if (!invalid)
                            AddToken(new StringToken(final, wide, chr, inc), l, c);
                        _includeDirective = false;
                    }
                    else
                    {
                        bool add = false;
                        foreach (string op in OperatorStrings.List)
                        {
                            switch (op.Length)
                            {
                                case 4: add = CheckAdvance(op[0], op[1], op[2], op[3]); break;
                                case 3: add = CheckAdvance(op[0], op[1], op[2]); break;
                                case 2: add = CheckAdvance(op[0], op[1]); break;
                                case 1: add = CheckAdvance(op[0]); break;
                            }
                            if (add)
                            {
                                AddToken(new OperatorToken(op), l, c);
                                break;
                            }
                        }
                        if (!add)
                        {
                            if (CheckAdvance(IdentifierLetters) != '\0')
                            {
                                while (CheckAdvance(IdentifierLetters) != '\0' ||
                                    CheckAdvance(IdentifierDigits) != '\0') ;
                                AddToken(new IdentifierToken(Code.Substring(start, _chindex - start)), l, c);
                            }
                            else if (Whitespace(Current()))
                            {
                                while (!End() && Whitespace(Current()))
                                {
                                    if (Current() == '\n')
                                    {
                                        AddToken(new NewlineMarker());
                                        _includeDirective = false;
                                    }
                                    Advance();
                                }
                            }
                            else
                                AddToken(new OtherToken(Advance().ToString()), l, c);
                        }
                    }
                }
            }
            AddToken(new NewlineMarker());
        }

        void PrepareTokensForParser()
        {
            List<Token> cut = new List<Token>();
            bool lineStarted = true, directive = false;
            int inExpansion = 0;
            int index = 0;
            string[] ignoredDirectives = { "pragma", "sccs", "ident", "line" };
            foreach (Token t in Output)
            {
                if (lineStarted && t is OperatorToken ot &&
                    (ot.Type == OperatorType.Hash || ot.Type == OperatorType.DiHash))
                {
                    if (index + 1 == Output.Count || !(Output[index + 1] is IdentifierToken it) ||
                        !ignoredDirectives.Contains(it.Value))
                        directive = true;
                }

                if (lineStarted = t is NewlineMarker)
                    directive = false;

                if (!directive)
                {
                    if (t is IdentifierToken it && KeywordStrings.List.Contains(it.Value))
                        cut.Add(new KeywordToken(it.Value, it.Filename,
                            it.Position.Line, it.Position.Column));
                    else if (t is MacroExpansionMarker m)
                        inExpansion++;
                    else if (t is MacroExpansionEndMarker)
                        inExpansion--;
                    else if (!(t is MacroExpansionMarker || t is MacroExpansionEndMarker || t is NewlineMarker))
                        cut.Add(t);
                }
                index++;
            }
            Output = cut;
        }
    }
}
