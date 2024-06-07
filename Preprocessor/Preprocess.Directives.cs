using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace cfcore.Preprocessor
{
    public partial class Preprocess
    {
        internal class StopException : Exception { }
        public List<string> IncludeDirectories { get; set; }
            = new List<string>();

        public Dictionary<string, Macro> Macros { get; set; }
            = new Dictionary<string, Macro>();

        int _tokindex = 0;
        string _filename = "";

        bool TokenEnd(int index) => index < 0 || index >= Output.Count;
        bool TokenEnd() => TokenEnd(_tokindex);

        Token TokenAt(int index)
            => TokenEnd(index) ? null : Output[index];
        Token TokenAtRel(int offset)
            => TokenEnd(_tokindex + offset) ? null : Output[_tokindex + offset];

        Token TokenPrevious() => TokenAt(_tokindex - 1);

        Token TokenCurrent()
        {
            Token t = TokenAt(_tokindex);
            if (t != null)
            {
                _filename = t.Filename;
                (_line, _col) = t.Position;
                Macros["__LINE__"].Expansion = new List<Token> { new NumberToken(_line.ToString()) };
                Macros["__FILE__"].Expansion = new List<Token> { new StringToken(_filename) };
            }
            return t;
        }

        Token TokenNext() => TokenAt(_tokindex + 1);

        Token TokenAdvance()
        {
            Token t = TokenCurrent();
            _tokindex++;
            return t;
        }

        T TokenCheck<T>() where T : Token
        {
            Token t = TokenCurrent();
            if (t is T)
                return t as T;
            return null;
        }

        T TokenCheck<T>(string value) where T : Token
        {
            Token t = TokenCurrent();
            if (t is T && t.Value == value)
                return t as T;
            return null;
        }

        T TokenEat<T>() where T : Token
        {
            Token t = TokenCheck<T>();
            if (t)
            {
                TokenAdvance();
                return t as T;
            }
            return null;
        }

        T TokenEat<T>(string value) where T : Token
        {
            Token t = TokenCheck<T>(value);
            if (t)
            {
                TokenAdvance();
                return t as T;
            }
            return null;
        }

        OperatorToken OperatorEat(OperatorType type)
        {
            var ot = TokenCheck<OperatorToken>();
            if (ot && ot.Type == type)
            {
                TokenAdvance();
                return ot;
            }
            return null;
        }

        OperatorToken OperatorCheck(OperatorType type)
        {
            var ot = TokenCheck<OperatorToken>();
            if (ot && ot.Type == type)
                return ot;
            return null;
        }

        void SkipUntilNewLine()
        {
            for (Token t = TokenCurrent(); !TokenEnd() && !(t is NewlineMarker);
                TokenAdvance(), t = TokenCurrent());
            TokenAdvance();
        }

        Stack<string> _dontExpand = new Stack<string>();

        void SetupPredefinedMacros()
        {
            Macros["__LINE__"] = new Macro(new List<Token>
            {
                new NumberToken("1", "predefined", 1, 1)
            });
            Macros["__FILE__"] = new Macro(new List<Token>
            {
                new StringToken(Filename, line: 2, col: 1, filename: "predefined")
            });
            Macros["__TIME__"] = new Macro(new List<Token>
            {
                new StringToken(DateTime.Now.ToString("H:mm:ss"), line: 3, col: 1, filename: "predefined")
            });
            Macros["__DATE__"] = new Macro(new List<Token>
            {
                new StringToken(DateTime.Now.ToString("Mmm dd yyyy"), line: 4, col: 1, filename: "predefined")
            });
            Macros["__TIMESTAMP__"] = new Macro(new List<Token>
            {
                new StringToken(File.GetLastWriteTime(FilePath).ToString("dd.MM.yyyy hh:mm:ss"),
                    line: 5, col: 1, filename: "predefined")
            });
            Macros["__STDC__"] = new Macro(new List<Token>
            {
                new NumberToken("1" /* Something about this doesn't feel right, but I'm not sure what. */,
                    line: 6, col: 1, filename: "predefined")
            });
            Macros["__STDC_VERSION__"] = new Macro(new List<Token>
            {
                new NumberToken(Standard == Standard.C99 ? "199901L" : "0L",
                    line: 7, col: 1, filename: "predefined")
            });
        }

        string Stringify(List<Token> tokens)
        {
            int token_len(Token t)
            {
                if (t is NumberToken || t is IdentifierToken || t is OperatorToken)
                    return t.Value.Length;
                else if (t is StringToken st)
                    return t.Value.Length + (st.Wide ? 1 : 0);
                return 0;
            }

            var stringified = new StringBuilder();
            for (int i = 0; tokens.Count > i; i++)
            {
                Token lastToken = i == 0 ? null : tokens[i - 1];
                Token current = tokens[i];

                if (!(current is NewlineMarker) && lastToken && (lastToken.Position.Line != current.Position.Line ||
                    lastToken.Position.Column + token_len(lastToken) != current.Position.Column))
                    stringified.Append(' ');

                switch (tokens[i])
                {
                    case NumberToken nt:
                        stringified.Append(nt.Value);
                        break;
                    case IdentifierToken it:
                        stringified.Append(it.Value);
                        break;
                    case OperatorToken ot:
                        stringified.Append(ot.Value);
                        break;
                    case StringToken st:
                        stringified.Append((st.Wide ? "L\"" : "\"") + st.Value + "\"");
                        break;
                }
            }
            return stringified.ToString();
        }

        /// <summary>
        /// Pastes two tokens together.
        /// </summary>
        Token Paste(Token a, Token b)
        {
            if (a == null && b == null)
                return null;
            if (a == null)
                return b;
            if (b == null)
                return a;
            // Number + number
            if (a is NumberToken n1 && b is NumberToken n2)
                return new NumberToken(n1.Value + n2.Value, a.Filename,
                    a.Position.Line, a.Position.Column);
            // Identifier + identifier
            else if (a is IdentifierToken i1 && b is IdentifierToken i2)
                return new IdentifierToken(i1.Value + i2.Value, a.Filename, a.Position.Line,
                    a.Position.Column);
            // Operator + operator
            else if (a is OperatorToken o1 && b is OperatorToken o2)
            {
                var op = new OperatorToken(o1.Value + o2.Value, a.Filename, a.Position.Line,
                    a.Position.Column);
                if (op.Type != (OperatorType)(-1))
                    return op;
            }
            // Number + identifier
            else if (a is NumberToken nt && b is IdentifierToken i)
                return new NumberToken(nt.Value + i.Value, a.Filename, a.Position.Line,
                    a.Position.Column);
            // Identifier + number
            else if (a is IdentifierToken _i && b is NumberToken _nt)
                return new IdentifierToken(_i.Value + _nt.Value, a.Filename, a.Position.Line,
                    a.Position.Column);
            // 'L' prefix + string literal
            else if (a is IdentifierToken __i && b is StringToken st && __i.Value == "L" && !st.Wide)
                return new StringToken(st.Value, true, st.Char, false, a.Position.Line,
                    a.Position.Column, a.Filename);
            Error("Tokens cannot be pasted together", a.Position.Line, a.Position.Column);
            return null;
        }

        bool ExpandMacros()
        {
        start:
            Token t;
            if (t = TokenCheck<IdentifierToken>())
            {
                var it = t as IdentifierToken;
                var originalPos = it.Position;

                if (!_dontExpand.Contains(it.Value) && Macros.ContainsKey(it.Value))
                {
                    Macro m = Macros[it.Value];
                    int pos = _tokindex;
                    if (m is FunctionMacro fm)
                    {
                        TokenAdvance();
                        if (!OperatorEat(OperatorType.LeftParen))
                        {
                            _tokindex--;
                            UpdateFilePosition();
                            return false;
                        }
                        else
                        {
                            bool variadic = fm.Arguments.Count > 0 && fm.Arguments.Last() == "...";
                            var args = new List<List<Token>>();
                            int parentheses = 1;
                            int currentArg = _tokindex;
                            while (parentheses > 0)
                            {
                                if (TokenEnd())
                                {
                                    Error("Unterminated macro call");
                                    break;
                                }
                                if (OperatorCheck(OperatorType.RightParen)
                                    || !(variadic && args.Count == fm.Arguments.Count - 1)
                                    && parentheses == 1 && OperatorCheck(OperatorType.Comma))
                                {
                                    args.Add(Output.GetRange(currentArg, _tokindex - currentArg));
                                    TokenAdvance();
                                    if ((TokenPrevious() as OperatorToken).Type == OperatorType.RightParen)
                                        break;
                                    currentArg = _tokindex;
                                }
                                else if (OperatorEat(OperatorType.LeftParen))
                                    parentheses++;
                                else if (OperatorEat(OperatorType.RightParen))
                                    parentheses--;
                                else if (!ExpandMacros())
                                    TokenAdvance();
                            }
                            Token lastCallToken = TokenPrevious();
                            if (!(fm.Arguments.Count == 0 && args.Count == 1 && args.Last().Count == 0)
                                && fm.Arguments.Count != args.Count)
                            {
                                if (!variadic)
                                    Error($"Macro {it.Value} takes {fm.Arguments.Count} arguments, but {args.Count} passed",
                                        it.Position.Line, it.Position.Column);
                                else if (fm.Arguments.Count - 1 > args.Count)
                                    Error($"Not enough non-variadic arguments passed",
                                        it.Position.Line, it.Position.Column);

                                for (int j = args.Count; fm.Arguments.Count > j; j++)
                                    args.Add(new List<Token>());
                            }

                            var expansion = fm.Expansion.ToList();
                            // Replace all occurrences of argument names with their expansions,
                            // do stringification and token pasting, handle variadic arguments.
                            List<Token> get_arg(string name) => (variadic && name == "__VA_ARGS__") 
                                ? args.Last() 
                                : fm.Arguments.Contains(name) ?
                                args[fm.Arguments.IndexOf(name)]
                                : null;
                            int i = 0;
                            while (i < expansion.Count)
                            {
                                if (expansion[i] is IdentifierToken argName)
                                {
                                    var arg = get_arg(argName.Value);
                                    if (arg != null)
                                    {
                                        expansion.RemoveAt(i);
                                        expansion.InsertRange(i, arg);
                                        i += arg.Count;
                                    }
                                    else
                                        i++;
                                }
                                else if (expansion[i] is OperatorToken ot)
                                {
                                    if (ot.Type == OperatorType.Hash || ot.Type == OperatorType.DiHash)
                                    {
                                        var arg = (i + 1 == expansion.Count) ? null :
                                            get_arg((expansion[i + 1] as IdentifierToken).Value);
                                        if (arg == null)
                                            Error("'#' not followed by a macro argument",
                                                ot.Position.Line, ot.Position.Column);
                                        else
                                        {
                                            string stringName = (expansion[i + 1] as IdentifierToken).Value;
                                            var (sLine, sCol) = expansion[i].Position;
                                            string sFname = expansion[i].Filename;
                                            expansion.RemoveAt(i);
                                            expansion[i] = new StringToken(Stringify(arg), line: sLine,
                                                col: sCol, filename: sFname);
                                            i++;
                                        }
                                    }
                                    else if (ot.Type == OperatorType.TokenPaste || ot.Type == OperatorType.DiTokenPaste)
                                    {
                                        int pasteStart = i, pasteLen = 1;
                                        bool missingLeft = i == 0,
                                            missingRight = i + 1 == expansion.Count;
                                        var expandedLeft = new List<Token>();
                                        var expandedRight = new List<Token>();
                                        Token left = missingLeft ? null : expansion[i - 1];
                                        Token right = missingRight ? null : expansion[i + 1];
                                        if (!left)
                                            Error("Token expected before '##'", ot.Position.Line, ot.Position.Column);
                                        else
                                        {
                                            pasteStart--;
                                            pasteLen++;
                                            if (!right)
                                                Error("Token expected after '##'", ot.Position.Line, ot.Position.Column);
                                            else
                                                pasteLen++;
                                        }

                                        var argLeft = left is IdentifierToken il ? get_arg(il.Value) : null;
                                        if (argLeft != null)
                                            expandedLeft.AddRange(argLeft);
                                        else if (left)
                                            expandedLeft.Add(left);

                                        var argRight = right is IdentifierToken ir ? get_arg(ir.Value) : null;
                                        if (argRight != null)
                                            expandedRight.AddRange(argRight);
                                        else if (left)
                                            expandedRight.Add(right);

                                        Token a = expandedLeft.Count > 0 ? expandedLeft.Last() : null;
                                        Token b = expandedRight.Count > 0 ? expandedRight.First() : null;
                                        Token pasted = Paste(a, b);
                                        if (pasted)
                                        {
                                            if (a)
                                                expandedLeft.RemoveAt(expandedLeft.Count - 1);
                                            if (b)
                                                expandedRight.RemoveAt(0);
                                        }
                                        expansion.RemoveRange(pasteStart, pasteLen);
                                        expansion.InsertRange(pasteStart, expandedLeft);
                                        int isPasted = pasted ? 1 : 0;
                                        if (pasted)
                                            expansion.Insert(pasteStart + expandedLeft.Count, pasted);
                                        expansion.InsertRange(pasteStart + expandedLeft.Count + isPasted, expandedRight);
                                        i = pasteStart + expandedLeft.Count + isPasted + expandedRight.Count;
                                    }
                                    else
                                        i++;
                                }
                                else
                                    i++;
                            }
                            expansion.Insert(0, new MacroExpansionMarker(it.Value,
                            it.Filename, it.Position.Line,
                            it.Position.Column));
                            expansion.Add(new MacroExpansionEndMarker(lastCallToken.Filename,
                                lastCallToken.Position.Line,
                                lastCallToken.Position.Column));
                            RemoveTokens(pos, _tokindex - pos);
                            InsertTokens(expansion, false);
                        }
                    }
                    else
                    {
                        Output.RemoveAt(_tokindex);
                        InsertToken(new MacroExpansionMarker(it.Value, it.Filename, originalPos.Line, originalPos.Column));
                        InsertTokens(m.Expansion.ToList());
                        InsertToken(new MacroExpansionEndMarker(it.Filename, originalPos.Line,
                            originalPos.Column + it.Value.Length));
                        _tokindex = pos;
                        UpdateFilePosition();
                    }
                    return true;
                }
            }
            else if (t = TokenEat<MacroExpansionMarker>())
            {
                var m = t as MacroExpansionMarker;
                _dontExpand.Push(m.Value);
                goto start;
            }
            else if (TokenEat<MacroExpansionEndMarker>())
            {
                _dontExpand.Pop();
                goto start;
            }
            return false;
        }

        void InsertTokens(List<Token> tokens, bool move = true)
        {
            Output.InsertRange(_tokindex, tokens);
            if (move) _tokindex += tokens.Count;
        }

        void InsertToken(Token token, bool move = true)
        {
            Output.Insert(_tokindex, token);
            if (move) _tokindex++;
        }

        void RemoveToken(int index, bool move = true)
        {
            Output.RemoveAt(index);
            if (move && index < _tokindex)
                _tokindex--;
        }

        void RemoveTokens(int index, int len, bool move = true)
        {
            for (int i = 0; len > i; i++)
                RemoveToken(index, true);
        }

        void UpdateFilePosition() => TokenCurrent();

        void ExpandUntilNewline(bool evalDefined = false)
        {
            int i = _tokindex;
            while (!TokenCheck<NewlineMarker>())
            {
                if (TokenEnd())
                    break;
                int start = _tokindex;
            start:
                IdentifierToken it = TokenCheck<IdentifierToken>();
                if (it)
                {
                    if (evalDefined && it.Value == "defined")
                    {
                        TokenAdvance();
                        while (TokenEat<MacroExpansionEndMarker>()) ;
                        bool parens = false;
                        if (OperatorEat(OperatorType.LeftParen)) parens = true;
                        IdentifierToken mName = null;
                        var val = new NumberToken("0",
                                it.Filename,
                                it.Position.Line, it.Position.Column);
                        if (!(mName = TokenEat<IdentifierToken>()))
                            Error("Expected macro name");
                        else if (Macros.ContainsKey(mName.Value))
                            val.Value = "1";
                        if (OperatorEat(OperatorType.RightParen)) parens = false;
                        if (parens)
                            Error("Expected ')' after 'defined'");
                        RemoveTokens(start, _tokindex - start);
                        InsertToken(val);
                    }
                    else if (ExpandMacros())
                        goto start;
                    else
                        TokenAdvance();
                }
                else
                    TokenAdvance();
            }
            for (int j = _tokindex; i <= j; j--)
                if (Output[j] is MacroExpansionMarker || 
                    Output[j] is MacroExpansionEndMarker)
                    RemoveToken(j, false);
            _tokindex = i;
            UpdateFilePosition();
        }

        void ProcessDirectives(bool inConditional = false, bool handle = true)
        {
            void Line(int line, string filename = "")
            {
                for (int i = _tokindex; Output.Count > i; i++)
                {
                    Token t = Output[i];
                    if (filename != "")
                        t.Filename = filename;
                    t.Position = (line, t.Position.Column);
                    if (t is NewlineMarker)
                        line++;
                }
            }

            _line = 1;
            _col = 1;

            while (!TokenEnd())
            {
                if (OperatorEat(OperatorType.Hash) || OperatorEat(OperatorType.DiHash))
                {
                    if (handle && TokenEat<IdentifierToken>("include"))
                    {
                        ExpandUntilNewline();
                        Token stringStart = TokenCurrent();
                        int start = _tokindex;
                        var st = TokenEat<StringToken>();
                        bool inc = st && st.IncludeDirectory;
                        if (!st)
                            Error("Expected <filename> or \"filename\"");
                        string path = null;
                        if (st)
                        {
                            if (!TokenEat<NewlineMarker>())
                            {
                                Warning("Extra tokens after #include directive");
                                SkipUntilNewLine();
                            }
                            if (!inc)
                            {
                                path = Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar + st.Value;
                                if (!File.Exists(path))
                                    Error($"Local file \"{st.Value}\" not found", stringStart.Position.Line,
                                        stringStart.Position.Column);
                            }
                            else
                            {
                                foreach (string s in IncludeDirectories)
                                {
                                    string p = s + Path.DirectorySeparatorChar + st.Value;
                                    if (File.Exists(p))
                                    {
                                        path = p;
                                        break;
                                    }
                                }
                                if (path == null)
                                    Error($"\"{st.Value}\" found in no include directories", stringStart.Position.Line,
                                        stringStart.Position.Column);
                            }

                            if (path != null && File.Exists(path))
                            {
                                int savedLine = stringStart.Position.Line;
                                string savedFile = stringStart.Filename;
                                Preprocess sub = new Preprocess(path, IncludeDirectories,
                                    Macros, Trigraphs, Standard);
                                sub.Do(included: true);
                                foreach (Warning w in sub.Warnings)
                                    Warnings.Add(w);
                                foreach (Error e in sub.Errors)
                                    Errors.Add(e);
                                InsertTokens(sub.Output, move: true);
                                InsertTokens(new List<Token>
                                {
                                    new NewlineMarker(),
                                    new OperatorToken("#"),
                                    new IdentifierToken("line"),
                                    new NumberToken(savedLine.ToString()),
                                    new StringToken(savedFile),
                                    new NewlineMarker()
                                }, move: false);

                                _tokindex -= sub.Output.Count;
                            }
                        }
                    }
                    else if (handle && TokenEat<IdentifierToken>("define"))
                    {
                        var name = TokenEat<IdentifierToken>();
                        if (!name)
                            Error("Expected macro name");
                        else
                        {
                            if (name.Value == "defined")
                            {
                                Error("Name of macro can't be 'defined'");
                                SkipUntilNewLine();
                            }
                            else
                            {
                                if (Macros.ContainsKey(name.Value))
                                {
                                    Macros.Remove(name.Value);
                                    Warning($"Redefinition of '{name}'");
                                }
                                List<string> args = null;
                                Token argStart = OperatorCheck(OperatorType.LeftParen);
                                if (argStart && argStart.Position.Column == name.Position.Column + name.Value.Length)
                                {
                                    args = new List<string>();
                                    TokenAdvance();
                                    do
                                    {
                                        Token argName = (Token)TokenEat<IdentifierToken>() ??
                                            TokenEat<OperatorToken>("...");
                                        if (!argName)
                                        {
                                            if (!OperatorCheck(OperatorType.RightParen))
                                                Error("Expected macro argument name");
                                        }
                                        else if (argName.Value == "..." && !OperatorCheck(OperatorType.RightParen))
                                            Error("Ellipsis is not last argument in macro declaration");
                                        else
                                            args.Add(argName.Value);
                                    }
                                    while (OperatorEat(OperatorType.Comma));
                                    if (!OperatorEat(OperatorType.RightParen))
                                        Error("Expected ')' after argument list");
                                }
                                int pos = _tokindex;
                                SkipUntilNewLine();
                                List<Token> expansion =
                                    Output.GetRange(pos, _tokindex - pos - 1);
                                Macros[name.Value] = args != null ? new FunctionMacro(args, expansion)
                                    : new Macro(expansion);
                            }
                        }
                    }
                    else if (handle && TokenEat<IdentifierToken>("undef"))
                    {
                        var name = TokenEat<IdentifierToken>();
                        if (!name)
                            Error("Expected macro name");
                        else if (!Macros.ContainsKey(name.Value))
                            Error($"Macro '{name.Value}' not defined");
                        else
                            Macros.Remove(name.Value);
                    }
                    else if (TokenEat<IdentifierToken>("ifdef") || TokenEat<IdentifierToken>("ifndef"))
                    {
                        bool ifndef = (TokenPrevious() as IdentifierToken).Value == "ifndef";
                        var name = TokenEat<IdentifierToken>();
                        bool isTrue = false;
                        if (!name)
                        {
                            if (handle) Error("Expected macro name");
                            isTrue = false;
                        }
                        else
                        {
                            isTrue = Macros.ContainsKey(name.Value) ^ ifndef;
                            if (handle && !TokenCheck<NewlineMarker>())
                                Warning($"Extra tokens at end of #{(ifndef ? "ifndef" : "ifdef")} directive");
                            SkipUntilNewLine();
                        }
                        int ifStart = _tokindex, ifLen = 0, elseStart = 0, elseLen = 0;
                        try
                        {
                            ProcessDirectives(true, handle && isTrue);
                        }
                        catch (StopException)
                        {
                        }
                        ifLen = _tokindex - ifStart;
                        if (!OperatorEat(OperatorType.Hash) && !OperatorEat(OperatorType.DiHash))
                        {
                            if (handle)
                                Error($"Unterminated #{(ifndef ? "ifndef" : "ifdef")} directive");
                        }
                        else
                        {
                            if (!TokenEat<IdentifierToken>("endif"))
                            {
                                if (TokenEat<IdentifierToken>("else"))
                                {
                                    SkipUntilNewLine();
                                    elseStart = _tokindex;
                                    try
                                    {
                                        ProcessDirectives(true, handle && !isTrue);
                                    }
                                    catch (StopException)
                                    {
                                    }
                                    elseLen = _tokindex - elseStart;
                                    if (!OperatorEat(OperatorType.Hash) && !OperatorEat(OperatorType.DiHash) && handle)
                                        Error($"Unterminated #{(ifndef ? "ifndef" : "ifdef")} directive");
                                    else if (!TokenEat<IdentifierToken>("endif"))
                                    {
                                        if (handle) Error("Expected #endif");
                                        SkipUntilNewLine();
                                    }
                                }
                                else if (TokenEat<IdentifierToken>("elif"))
                                {
                                    if (handle) Error("#elif directive not allowed in #ifdef/#ifndef");
                                    SkipUntilNewLine();
                                }
                            }
                            else
                                SkipUntilNewLine();

                            if (handle)
                            {
                                if (isTrue && elseStart > 0)
                                    RemoveTokens(elseStart, elseLen);
                                if (!isTrue)
                                    RemoveTokens(ifStart, ifLen);
                            }
                        }
                    }
                    else if (TokenCheck<IdentifierToken>("if"))
                    {
                        _tokindex--;
                        UpdateFilePosition();
                        int trueIndex = -1;
                        bool stop = false;

                        var blocks = new List<(int start, int len)>();

                        for (int i = 0; ; i++)
                        {
                            if (TokenEnd())
                            {
                                Error("Unterminated #if directive");
                                break;
                            }

                            if (!OperatorEat(OperatorType.Hash))
                                OperatorEat(OperatorType.DiHash);

                            var it = TokenEat<IdentifierToken>();

                            if (!it)
                            {
                                Error("Unterminated #if directive");
                                break;
                            }
                            bool @else = it.Value == "else";
                            bool endif = it.Value == "endif";

                            bool isTrue = false;
                            if (!endif)
                            {
                                if (@else)
                                    isTrue = trueIndex == -1;
                                else
                                {
                                    ExpandUntilNewline(evalDefined: true);
                                    isTrue = ParseAndEvaluate() != 0;
                                }
                            }
                            if (!TokenCheck<NewlineMarker>())
                                Warning($"Extra tokens at end of #{it.Value} directive");
                            SkipUntilNewLine();
                            int start = _tokindex, len = 0;

                            if (!endif)
                            {
                                try
                                {
                                    ProcessDirectives(true, handle && isTrue);
                                    len = _tokindex - start;
                                }
                                catch (StopException)
                                {
                                    stop = true;
                                }
                                if (stop)
                                    break;

                                if (trueIndex == -1 && isTrue)
                                    trueIndex = i;
                                blocks.Add((start, len));
                            }
                            else
                                break;
                        }

                        for (int i = blocks.Count - 1; 0 <= i; i--)
                        {
                            if (trueIndex != i)
                                RemoveTokens(blocks[i].start, blocks[i].len);
                        }
                    }
                    else if (handle && TokenEat<IdentifierToken>("error"))
                    {
                        int start = _tokindex;
                        SkipUntilNewLine();
                        Error($"#error {Stringify(Output.GetRange(start, _tokindex - 1 - start))}");
                        throw new StopException();
                    }
                    else if (handle && TokenEat<IdentifierToken>("warning"))
                    {
                        int start = _tokindex;
                        SkipUntilNewLine();
                        Warning($"#warning {Stringify(Output.GetRange(start, _tokindex - 1 - start))}");
                    }
                    else if (handle && TokenEat<IdentifierToken>("line"))
                    {
                        var lineTok = TokenEat<NumberToken>();
                        if (!lineTok || !int.TryParse(lineTok.Value, out int lineNum))
                            Error("Expected line number (as positive integer)");
                        else
                        {
                            var fileTok = TokenEat<StringToken>();
                            string fileName = fileTok ? fileTok.Value : "";
                            if (!TokenCheck<NewlineMarker>())
                                Warning("Extra tokens at the end of #line directive");
                            SkipUntilNewLine();
                            Line(lineNum, fileName);
                        }
                    }
                    else if (inConditional && (TokenCheck<IdentifierToken>("endif") ||
                        TokenCheck<IdentifierToken>("else") ||
                        TokenCheck<IdentifierToken>("elif")))
                    {
                        _tokindex--;
                        UpdateFilePosition();
                        return;
                    }
                    else
                        TokenAdvance();
                }
                else
                {
                    while (ExpandMacros());
                    TokenAdvance();
                }
            }
        }
    }
}