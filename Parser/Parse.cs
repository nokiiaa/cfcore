using cfcore.Preprocessor;
using cfcore.Parser.AST;
using System;
using System.Collections.Generic;
using System.Threading;

namespace cfcore.Parser
{
    public class Parse
    {
        internal enum ParseErrorID
        {
            UnexpectedToken = 1,
            DeclarationSemicolon = 2,
            ExpressionAfterAssign = 3,
            ExpressionAfterColon = 4,
            ColonAfterLabel = 5,
            CaseLabelExpr = 6,
            IfLeftParen = 7,
            IfRightParen = 8,
            IfCondition = 9,
            SwitchLeftParen = 10,
            SwitchCondition = 11,
            SwitchRightParen = 12,
            WhileLeftParen = 13,
            WhileCondition = 14,
            WhileRightParen = 15,
            WhileExpected = 16,
            DoWhileLeftParen = 17,
            DoWhileCondition = 18,
            DoWhileRightParen = 19,
            StatementSemicolon = 20,
            ForLeftParen = 21,
            ForRightParen = 22,
            ForInitializerSemicolon = 23,
            ForConditionSemicolon = 24,
            MultipleStorageClasses = 25,
            DisallowedStorageClass = 26,
            StructUnionType = 27,
            EnumType = 28,
            EnumNameExpected = 29,
            EnumValueExpected = 30,
            StructUnionRightBrace = 31,
            EnumRightBrace = 32,
            SingleVarArg = 33,
            NonLastVarArg = 34,
            ArgListRightParen = 35,
            StaticNotAllowed = 36,
            ArrayRightSquare = 37,
            QualifierNotAllowed = 38,
            DesignatorExprExpected = 39,
            DesignatorRightSquare = 40,
            DesignatorExpr = 41,
            IdentifierAfterDot = 42,
            TernaryQuestionMarkExpected = 43,
            CastExprExpected = 44,
            CastRightParen = 45,
            SizeofExprExpected = 46,
            TypeRightParen = 47,
            MemberNameExpected = 48,
            CallRightParen = 49,
            CallArgument = 50,
            IndexRightSquare = 51,
            IndexExpr = 52,
            InitializerListRightBrace = 53,
            InitializerListExpression = 54,
            AssignAfterDesignator = 65,
            InvalidDeclaration = 66,
            AbruptEOF = 67,
            RightOperandExpected = 68,
            StrConcat = 69,
        }

        /// <summary>
        /// The C standard to assume.
        /// </summary>
        public Standard Standard { get; set; }
        /// <summary>
        /// The list of the names type definitions encountered in the
        /// source file after calling Do().
        /// </summary>
        public NameScope Typedefs { get; set; }
        public List<Token> Input { get; set; }

        /// <summary>
        /// The list of warnings produced after calling Do().
        /// </summary>
        public List<Warning> Warnings { get; set; }
        /// <summary>
        /// The list of errors produced after calling Do().
        /// </summary>
        public List<Error> Errors { get; set; }
        /// <summary>
        /// The AST (abstract syntax tree) produced after calling Do().
        /// </summary>
        public TranslationUnit Output { get; set; }
        int Index { get; set; } = 0;
        (int Line, int Column) Position;
        (int index, (int, int) pos, int tsle, string filename) State 
            => (Index, Position, _tokensSinceLastError, Filename);
        bool End => Index >= Input.Count;
        void SetState((int, (int, int), int, string) state) 
            => (Index, Position, _tokensSinceLastError, Filename) = state;

        private int _level = 0;
        private ParseErrorID _lastError = 0;
        private int _tokensSinceLastError = 0;

        /// <summary>
        /// The name of the file being parsed.
        /// </summary>
        public string Filename { get; set; }

        Token At(int index) 
            => index < 0 || index >= Input.Count ? null : Input[index];
        Token AtRel(int offset) =>
            At(Index + offset);
        Token Previous() => AtRel(-1);
        Token Current() => AtRel(0);

        Token Advance()
        {
            Token t = At(Index++);
            _tokensSinceLastError++;
            if (t != null)
            {
                Token c = Current();
                if (c != null)
                    SetState((Index, (c.Position.Line, c.Position.Column), _tokensSinceLastError, c.Filename));
            }
            return t;
        }
        
        bool CheckOperator(OperatorType type)
            => Current() is OperatorToken o && o.Type == type;
        bool Operator(OperatorType type)
            => CheckOperator(type) && Advance() is var _;

        OperatorType CheckOperator(OperatorType[] types)
        {
            foreach (var type in types)
                if (CheckOperator(type))
                    return (Current() as OperatorToken).Type;
            return OperatorType.None;
        }

        OperatorType Operator(OperatorType[] types)
            => CheckOperator(types) != OperatorType.None
            ? (Advance() as OperatorToken).Type 
            : OperatorType.None;

        bool CheckKeyword(KeywordType type)
            => Current() is KeywordToken k && k.Type == type;
        bool Keyword(KeywordType type)
            => CheckKeyword(type) && Advance() is var _;

        KeywordType CheckKeyword(KeywordType[] types)
        {
            foreach (var type in types)
                if (CheckKeyword(type))
                    return (Current() as KeywordToken).Type;
            return KeywordType.None;
        }

        KeywordType Keyword(KeywordType[] types)
            => CheckKeyword(types) != KeywordType.None
            ? (Advance() as KeywordToken).Type
            : KeywordType.None;

        T Check<T>() where T : Token
        {
            if (Current() is T)
                return Current() as T;
            return null;
        }

        T Eat<T>() where T : Token
        {
            if (Current() is T)
                return Advance() as T;
            return default(T);
        }

        /// <param name="filename">The name of the source file to be parsed.</param>
        /// <param name="input">The tokenized source file to produce the AST out of.</param>
        /// <param name="standard">The C standard to assume while parsing.</param>
        public Parse(string filename, List<Token> input, Standard standard = Standard.C99)
        {
            Standard = standard;
            Input = input;
            Filename = filename;
        }

        /// <summary>
        /// Parses the source file.
        /// </summary>
        /// <returns>The produced AST.</returns>
        public TranslationUnit Do()
        {
            Index = 0;
            Position = (1, 1);
            Typedefs = new NameScope();
            Warnings = new List<Warning>();
            Errors = new List<Error>();
            Output = new TranslationUnit();
            while (!End)
            {
                Node node = (Node)Function() ?? Declaration();
                if (node != null)
                    Output.Nodes.Add(node);
                else if (!End)
                {
                    Error("Unexpected token", ParseErrorID.UnexpectedToken);
                    Advance();
                }
            }
            return Output;
        }

        FuncDef Function()
        {
            var state = State;
            (int, int) pos = Position;
            string fname = Filename;
            var specs = TypeBaseSpecifiers();
            if (specs.type == null && specs.storage == StorageClass.None && !specs.inline)
                return null;
            var declarator = Declarator();
            if (declarator == null)
            {
                SetState(state);
                return null;
            }
            CType type = declarator.ToType(specs.type);
            if (!CheckOperator(OperatorType.Comma) && !CheckOperator(OperatorType.Semicolon)
                && type is CFunctionType && _level == 0) // Probably a function
            {
                List<Node> items = new List<Node>();
                do
                {
                    if (End)
                    {
                        Error("Reached EOF during declaration parsing", ParseErrorID.AbruptEOF);
                        return null;
                    }
                    Decl d = Declaration();
                    if (d != null)
                        items.Add(d);
                    else if (!CheckOperator(OperatorType.LeftBrace) && !CheckOperator(OperatorType.DiLeftBrace))
                    {
                        Error("Invalid declaration before function body", ParseErrorID.InvalidDeclaration);
                        Advance();
                    }
                }
                while (!CheckOperator(OperatorType.LeftBrace) && !CheckOperator(OperatorType.DiLeftBrace));
                var block = Block() as BlockStmt;
                if (block == null)
                {
                    SetState(state);
                    return null;
                }
                block.Items.InsertRange(0, items);
                var funcType = type as CFunctionType;
                string name = declarator.GetName();
                return new FuncDef(pos, fname, new CFunctionType(funcType.ReturnType, funcType.ArgumentList), 
                    name, block, specs.inline);
            }
            else
            {
                SetState(state);
                return null;
            }
        }

        Decl Declaration()
        {
            (int, int) pos = Position;
            string fname = Filename;
            var specs = TypeBaseSpecifiers();
            if (specs.type == null && specs.storage == StorageClass.None && !specs.inline)
                return null;
            Decl decl = new Decl(pos, fname, specs.storage, inline: specs.inline);
            do
            {
                var declarator = Declarator();
                Expr initializer = null;
                if (declarator == null)
                {
                    if (!Operator(OperatorType.Semicolon))
                        Error("';' expected after declaration", ParseErrorID.DeclarationSemicolon);
                    decl.Declared.Add((specs.type, "", null));
                    return decl;
                }
                else if (Operator(OperatorType.Assign))
                {
                    if (CheckOperator(OperatorType.LeftBrace) || CheckOperator(OperatorType.DiLeftBrace))
                        initializer = InitializerList();
                    else
                        initializer = Assignment();
                    if (initializer == null)
                        Error("Expected expression after '='", ParseErrorID.ExpressionAfterAssign);
                }
                string name = declarator.GetName();
                if (specs.storage == StorageClass.Typedef)
                    Typedefs[name] = true;
                CType type = declarator.ToType(specs.type);
                decl.Declared.Add((type, name, initializer));
            }
            while (Operator(OperatorType.Comma));
            if (!Operator(OperatorType.Semicolon))
                Error("';' expected after declaration", ParseErrorID.DeclarationSemicolon);
            return decl;
        }

        StructUnionDecl StructDecl()
        {
            (int, int) pos = Position;
            string fname = Filename;
            var specs = TypeBaseSpecifiers(false);
            if (specs.type == null && !specs.inline)
                return null;
            var decl = new StructUnionDecl(pos, fname, inline: specs.inline);
            do
            {
                var declarator = Declarator();
                Expr bitfieldLength = null;
                if (Operator(OperatorType.Colon))
                {
                    bitfieldLength = Ternary();
                    if (bitfieldLength == null)
                        Error("Expected expression after ':'", ParseErrorID.ExpressionAfterColon);
                }
                string name = declarator == null ? null : declarator.GetName();
                CType type = declarator == null ? specs.type : declarator.ToType(specs.type);
                decl.Declared.Add((type, name, bitfieldLength));
            }
            while (Operator(OperatorType.Comma));
            if (!Operator(OperatorType.Semicolon))
                Error("';' expected after declaration", ParseErrorID.DeclarationSemicolon);
            return decl;
        }

        Stmt Block()
        {
            Typedefs = Typedefs.Down;
            (int, int) pos = Position;
            string fname = Filename;
            Advance();
            _level++;
            List<Node> items = new List<Node>();
            do
            {
                Node item = (Node)Declaration() ?? Statement();
                if (item == null && !End)
                {
                    Error("Unexpected token", ParseErrorID.UnexpectedToken);
                    Advance();
                }
                else
                    items.Add(item);
            }
            while (!Operator(OperatorType.RightBrace) && !Operator(OperatorType.DiRightBrace));
            _level--;
            Typedefs = Typedefs.Up;
            return new BlockStmt(pos, fname, items);
        }

        Stmt Statement()
        {
            (int, int) pos = Position;
            string fname = Filename;
            var state = State;
            IdentifierToken i;
            if (Keyword(KeywordType.Default))
            {
                if (!Operator(OperatorType.Colon))
                    Error("Expected ':' after 'default'", ParseErrorID.ColonAfterLabel);
                return new LabeledStmt(pos, fname, new DefaultLabel(), Statement());
            }
            else if (Keyword(KeywordType.Case))
            {
                Expr activation = Ternary();
                if (activation == null)
                    Error("Expected case label activation value", ParseErrorID.CaseLabelExpr);
                if (!Operator(OperatorType.Colon))
                    Error("Expected ':' after case label", ParseErrorID.ColonAfterLabel);
                return new LabeledStmt(pos, fname, new CaseLabel(activation), Statement());
            }
            else if (i = Eat<IdentifierToken>())
            {
                if (Operator(OperatorType.Colon))
                    return new LabeledStmt(pos, fname, new NamedLabel(i.Value), Statement());
                else
                    SetState(state);
            }
            else if (CheckOperator(OperatorType.LeftBrace) || CheckOperator(OperatorType.DiLeftBrace))
                return Block();
            else if (Keyword(KeywordType.If))
            {
                if (!Operator(OperatorType.LeftParen))
                    Error("Expected '(' after 'if'", ParseErrorID.IfLeftParen);
                Expr condition = Expr();
                if (condition == null)
                    Error("Expected 'if' condition", ParseErrorID.IfCondition);
                else if (!Operator(OperatorType.RightParen))
                    Error("Expected ')' after condition", ParseErrorID.IfRightParen);
                Stmt @if = Statement(), @else = null;
                if (Keyword(KeywordType.Else))
                    @else = Statement();
                return new IfStmt(pos, fname, condition, @if, @else);
            }
            else if (Keyword(KeywordType.Switch))
            {
                if (!Operator(OperatorType.LeftParen))
                    Error("Expected '(' after 'switch'", ParseErrorID.SwitchLeftParen);
                Expr condition = Expr();
                if (condition == null)
                    Error("Expected 'switch' condition", ParseErrorID.SwitchCondition);
                else if (!Operator(OperatorType.RightParen))
                    Error("Expected ')' after condition", ParseErrorID.SwitchRightParen);
                Stmt code = Statement();
                return new SwitchStmt(pos, fname, condition, code);
            }
            else if (Keyword(KeywordType.While))
            {
                if (!Operator(OperatorType.LeftParen))
                    Error("Expected '(' after 'while'", ParseErrorID.WhileLeftParen);
                Expr condition = Expr();
                if (condition == null)
                    Error("Expected 'while' condition", ParseErrorID.WhileCondition);
                else if (!Operator(OperatorType.RightParen))
                    Error("Expected ')' after condition", ParseErrorID.WhileRightParen);
                Stmt code = Statement();
                return new WhileStmt(pos, fname, condition, code);
            }
            else if (Keyword(KeywordType.Do))
            {
                Stmt code = Statement();
                if (!Keyword(KeywordType.While))
                    Error("Expected 'while' after do...while body", ParseErrorID.WhileExpected);
                if (!Operator(OperatorType.LeftParen))
                    Error("Expected '(' after 'while'", ParseErrorID.DoWhileLeftParen);
                Expr condition = Expr();
                if (condition == null)
                    Error("Expected do...while condition", ParseErrorID.DoWhileCondition);
                else if (!Operator(OperatorType.RightParen))
                    Error("Expected ')' after condition", ParseErrorID.DoWhileRightParen);
                if (!Operator(OperatorType.Semicolon))
                    Error("Expected ';' after do...while condition", ParseErrorID.StatementSemicolon);
                return new DoWhileStmt(pos, fname, condition, code);
            }
            else if (Keyword(KeywordType.For))
            {
                if (!Operator(OperatorType.LeftParen))
                    Error("Expected '(' after 'for'", ParseErrorID.ForLeftParen);
                Decl decl = Declaration();
                Expr initial = null, condition = null, advance = null;
                if (decl == null)
                {
                    initial = Expr();
                    if (!Operator(OperatorType.Semicolon))
                        Error("Expected ';' after 'for' initializer", ParseErrorID.ForInitializerSemicolon);
                }
                condition = Expr();
                if (!Operator(OperatorType.Semicolon))
                    Error("Expected ';' after 'for' condition", ParseErrorID.ForConditionSemicolon);
                advance = Expr();
                if (!Operator(OperatorType.RightParen))
                    Error("Expected ')' after 'for' header", ParseErrorID.ForRightParen);
                Stmt code = Statement();
                if (decl != null)
                    return new ForStmt(pos, fname, decl, condition, advance, code);
                else
                    return new ForStmt(pos, fname, initial, condition, advance, code);
            }
            else if (Keyword(KeywordType.Return))
            {
                Expr value = Expr();
                if (!Operator(OperatorType.Semicolon))
                    Error("Expected ';' after return statement", ParseErrorID.StatementSemicolon);
                return new ReturnStmt(pos, fname, value);
            }
            else if (Keyword(KeywordType.Break))
            {
                if (!Operator(OperatorType.Semicolon))
                    Error("Expected ';' after break statement", ParseErrorID.StatementSemicolon);
                return new BreakStmt(pos, fname);
            }
            else if (Keyword(KeywordType.Continue))
            {
                if (!Operator(OperatorType.Semicolon))
                    Error("Expected ';' after continue statement", ParseErrorID.StatementSemicolon);
                return new ContinueStmt(pos, fname);
            }
            Expr expr = Expr();
            if (!Operator(OperatorType.Semicolon) && !CheckOperator(OperatorType.RightBrace))
            {
                Error("Expected ';' after expression statement", ParseErrorID.StatementSemicolon);
                if (expr == null)
                    return null;
            }
            return new ExprStmt(pos, fname, expr);
        }

        (bool inline, StorageClass storage, CType type) TypeBaseSpecifiers(bool allowStorageClass = true)
        {
            bool inline = false;
            var qualifiers = CTypeQualifiers.None;
            CType ret = null;
            var qualifier = CTypeQualifiers.None;
            var storageClass = StorageClass.None;
            while (true)
            {
                qualifier = TypeQualifier();
                qualifiers |= qualifier;
                var sClass = GetStorageClass();
                if (allowStorageClass)
                {
                    if (sClass != StorageClass.None)
                        if (storageClass != StorageClass.None)
                            Error("Declaration cannot have more than one storage class", 
                                ParseErrorID.MultipleStorageClasses);
                        else
                            storageClass = sClass;
                }
                else if (sClass != StorageClass.None)
                    Error("Storage class not allowed here", ParseErrorID.DisallowedStorageClass);
                if (Keyword(KeywordType.Inline))
                    inline = true;
                CType spec = null;
                if (ret is CPrimitiveType || ret is null)
                {
                    var state = State;
                    spec = TypeSpecifier();
                    if (spec != null)
                    {
                        if (ret == null)
                            ret = spec;
                        else if ((ret == null || ret is CPrimitiveType) && spec is CPrimitiveType cpt)
                        {
                            if (ret == null)
                                ret = spec;
                            else
                            {
                                var retcpt = ret as CPrimitiveType;
                                if ((retcpt.Specifiers & cpt.Specifiers
                                    & CPrimitiveTypeSpecifiers.Long) != 0)
                                    retcpt.Specifiers |= CPrimitiveTypeSpecifiers.LongLong;
                                retcpt.Specifiers |= cpt.Specifiers;
                            }
                        }
                        else
                        {
                            spec = null;
                            SetState(state);
                        }
                    }
                }
                if (qualifier == CTypeQualifiers.None &&
                    sClass == StorageClass.None &&
                    spec == null)
                    break;
            }
            if (ret != null)
                ret.Qualifiers = qualifiers;
            return (inline, storageClass, ret);
        }

        CStructUnionType FinishStructUnion(bool union = false)
        {
            string name = "";
            IdentifierToken n;
            if (n = Eat<IdentifierToken>())
                name = n.Value;
            if (!Operator(OperatorType.LeftBrace) && !Operator(OperatorType.DiLeftBrace))
            {
                if (name == null)
                {
                    Error("Struct/union type must have at least a body or a name", ParseErrorID.StructUnionType);
                    return null;
                }
                return new CStructUnionType(CTypeQualifiers.None, union, name);
            }
            else
            {
                var members = new List<StructUnionDecl>();
                do
                {
                    var decl = StructDecl();
                    if (decl == null && !End && !CheckOperator(OperatorType.RightBrace) 
                        && !CheckOperator(OperatorType.DiRightBrace))
                    {
                        Error("Unexpected token", ParseErrorID.UnexpectedToken);
                        Advance();
                    }
                    else
                        members.Add(decl);
                }
                while (!End && !CheckOperator(OperatorType.RightBrace) 
                    && !CheckOperator(OperatorType.DiRightBrace));
                if (!Operator(OperatorType.RightBrace) && !Operator(OperatorType.DiRightBrace))
                    Error("Expected '}' after struct/union", ParseErrorID.StructUnionRightBrace);
                return new CStructUnionType(CTypeQualifiers.None, union, members, name);
            }
        }

        CEnumType FinishEnum()
        {
            string name = "";
            IdentifierToken n;
            if (n = Eat<IdentifierToken>())
                name = n.Value;
            if (!Operator(OperatorType.LeftBrace) && !Operator(OperatorType.DiLeftBrace))
            {
                if (name == null)
                {
                    Error("Enum type must have at least a body or a name", ParseErrorID.EnumType);
                    return null;
                }
                return new CEnumType(CTypeQualifiers.None, name);
            }
            else
            {
                var constants = new List<(string, Expr)>();
                do
                {
                    Expr value = null;
                    var cName = Eat<IdentifierToken>();
                    if (cName == null)
                    {
                        if (!CheckOperator(OperatorType.DiRightBrace)
                            && !CheckOperator(OperatorType.RightBrace))
                            Error("Expected constant name", ParseErrorID.EnumNameExpected);
                    }
                    else
                    {
                        if (Operator(OperatorType.Assign))
                        {
                            value = Ternary();
                            if (value == null)
                                Error("Expected value of constant", ParseErrorID.EnumValueExpected);
                        }
                        constants.Add((cName.Value, value));
                    }
                }
                while (Operator(OperatorType.Comma));
                if (!Operator(OperatorType.DiRightBrace)
                    && !Operator(OperatorType.RightBrace))
                    Error("Expected '}' after enum constant list", ParseErrorID.EnumRightBrace);
                return new CEnumType(CTypeQualifiers.None, constants, name);
            }
        }

        CType TypeSpecifier()
        {
            var state = State;
            IdentifierToken i = null;
            if (i = Eat<IdentifierToken>())
            {
                if (!Typedefs[i.Value])
                    SetState(state);
                else
                    return new CNamedType(CTypeQualifiers.None, i.Value);
            }
            else if (Keyword(KeywordType.Struct) || Keyword(KeywordType.Union))
                return FinishStructUnion((Previous() as KeywordToken).Type == KeywordType.Union);
            else if (Keyword(KeywordType.Enum))
                return FinishEnum();
            var cpt =
                new CPrimitiveType(CTypeQualifiers.None,
                    CPrimitiveTypeSpecifiers.None);
            if (Keyword(KeywordType.Void))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Void;
            else if (Keyword(KeywordType.Char))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Char;
            else if (Keyword(KeywordType.Short))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Short;
            else if (Keyword(KeywordType.Int))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Int;
            else if (Keyword(KeywordType.Long))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Long;
            else if (Keyword(KeywordType.Float))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Float;
            else if (Keyword(KeywordType.Double))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Double;
            else if (Keyword(KeywordType.Signed))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Signed;
            else if (Keyword(KeywordType.Unsigned))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Unsigned;
            else if (Keyword(KeywordType.Bool))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Bool;
            else if (Keyword(KeywordType.Complex))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Complex;
            else if (Keyword(KeywordType.Imaginary))
                cpt.Specifiers = CPrimitiveTypeSpecifiers.Imaginary;
            else
                return null;
            return cpt;
        }

        CTypeQualifiers TypeQualifier()
        {
            if (Keyword(KeywordType.Const))
                return CTypeQualifiers.Const;
            else if (Keyword(KeywordType.Restrict))
                return CTypeQualifiers.Restrict;
            else if (Keyword(KeywordType.Volatile))
                return CTypeQualifiers.Volatile;
            return CTypeQualifiers.None;
        }

        StorageClass GetStorageClass()
        {
            if (Keyword(KeywordType.Static))
                return StorageClass.Static;
            else if (Keyword(KeywordType.Auto))
                return StorageClass.Auto;
            else if (Keyword(KeywordType.Extern))
                return StorageClass.Extern;
            else if (Keyword(KeywordType.Register))
                return StorageClass.Register;
            else if (Keyword(KeywordType.Typedef))
                return StorageClass.Typedef;
            return StorageClass.None;
        }

        List<string> ParameterIdentifierList()
        {
            var state = State;
            var identifiers = new List<string>();
            do
            {
                var i = Eat<IdentifierToken>();
                if (i == null)
                {
                    SetState(state);
                    return null;
                }
                identifiers.Add(i.Value);
            }
            while (Operator(OperatorType.Comma));
            if (!CheckOperator(OperatorType.RightParen))
                return null;
            return identifiers;
        }

        CTypeQualifiers TypeQualifierList()
        {
            var qualifier = CTypeQualifiers.None;
            var qualifiers = CTypeQualifiers.None;
            while ((qualifier = TypeQualifier()) != CTypeQualifiers.None)
                qualifiers |= qualifier;
            return qualifiers;
        }

        Declarator DirectDeclarator(bool @abstract = false)
        {
            var state = State;
            Declarator decl = null;
            IdentifierToken i;
            if (i = Eat<IdentifierToken>())
            {
                if (@abstract)
                {
                    SetState(state);
                    return null;
                }
                else
                    decl = new NameDeclarator(i.Value);
            }
            else if (Operator(OperatorType.LeftParen))
            {
                Token t = Current();
                decl = new GroupDeclarator(Declarator(@abstract));
                if (!Operator(OperatorType.RightParen))
                {
                    SetState(state);
                    return null;
                }
            }

            if (!@abstract)
                if (decl == null || (decl is GroupDeclarator gd && gd.Declarator == null))
                    return null;
            while (true)
            {
                if (Operator(OperatorType.LeftParen))
                {
                    var args = new List<(CType, string)>();
                    var identifiers = ParameterIdentifierList();
                    int count = 0;
                    if (identifiers == null)
                    {
                        do
                        {
                            if (Operator(OperatorType.Ellipsis))
                            {
                                bool comma = CheckOperator(OperatorType.Comma);
                                if (count != 0 && !comma)
                                {
                                    args.Add((null, "..."));
                                    break;
                                }
                                else
                                {
                                    if (count == 0)
                                        Error("Function with variable arguments must have at least one non-variable argument",
                                            ParseErrorID.SingleVarArg);
                                    else if (comma)
                                        Error("'...' must come last in function parameter list",
                                            ParseErrorID.NonLastVarArg);
                                    continue;
                                }
                            }
                            if (CheckOperator(OperatorType.RightParen))
                                break;
                            var @base = TypeBaseSpecifiers(false);
                            var declarator = Declarator(false);
                            if (declarator == null)
                                declarator = Declarator(true);
                            string name = declarator == null ? null : 
                                declarator.GetName();
                            args.Add((declarator == null ? @base.type : 
                                declarator.ToType(@base.type), name));
                            count++;
                        }
                        while (Operator(OperatorType.Comma));
                    }
                    else
                        foreach (string name in identifiers)
                            args.Add((null, name));
                    if (!Operator(OperatorType.RightParen))
                        Error("Expected ')' after parameter list", ParseErrorID.ArgListRightParen);
                    decl = new FuncDeclarator(decl, args);
                }
                else if (Operator(OperatorType.DiLeftSquare) || Operator(OperatorType.LeftSquare))
                {
                    Expr expr = null;
                    bool vla = false, @static = false;
                    var qualifiers = CTypeQualifiers.None;
                    if (Keyword(KeywordType.Static))
                    {
                        if (@abstract)
                            Error("'static' not allowed in abstract declarator", ParseErrorID.StaticNotAllowed);
                        else
                            @static = true;
                    }
                    qualifiers = TypeQualifierList();
                    if (qualifiers != CTypeQualifiers.None && @abstract)
                    {
                        Error("Type qualifiers not allowed in abstract declarator", ParseErrorID.QualifierNotAllowed);
                        qualifiers = CTypeQualifiers.None;
                    }
                    if (Keyword(KeywordType.Static))
                    {
                        if (@static)
                            Error("'static' keyword encountered twice", ParseErrorID.StaticNotAllowed);
                        else if (@abstract)
                            Error("'static' not allowed in abstract declarator", ParseErrorID.StaticNotAllowed);
                        else
                            @static = true;
                    }
                    if (Operator(OperatorType.Multiply))
                        vla = true;
                    else
                        expr = Assignment();
                    if (!Operator(OperatorType.DiRightSquare) &&
                        !Operator(OperatorType.RightSquare))
                        Error("Expected ']' after array length", ParseErrorID.ArrayRightSquare);
                    if (vla)
                        decl = new ArrayDeclarator(decl, qualifiers, @static, vlaAsterisk: true);
                    else
                        decl = new ArrayDeclarator(expr, qualifiers, @static, decl);
                }
                else
                    break;
            }

            return decl;
        }

        Declarator Declarator(bool @abstract = false)
        {
            if (Operator(OperatorType.Multiply))
            {
                var ptrQualifiers = CTypeQualifiers.None;
                var kw = KeywordType.None;
                while ((kw = Keyword(new[] { KeywordType.Const, KeywordType.Volatile, KeywordType.Restrict }))
                    != KeywordType.None)
                {
                    switch (kw)
                    {
                        case KeywordType.Const:    ptrQualifiers |= CTypeQualifiers.Const;    break;
                        case KeywordType.Volatile: ptrQualifiers |= CTypeQualifiers.Volatile; break;
                        case KeywordType.Restrict: ptrQualifiers |= CTypeQualifiers.Restrict; break;
                    }
                }
                var declarator = Declarator(@abstract);
                if (declarator == null)
                    return null;
                return new PtrDeclarator(declarator, ptrQualifiers);
            }
            return DirectDeclarator(@abstract);
        }

        CType TypeName()
        {
            var specs = TypeBaseSpecifiers(false);
            if (specs.type == null)
                return null;
            var decl = Declarator(true);
            if (decl == null)
                return specs.type;
            return decl.ToType(specs.type);
        }

        Designator Designator(Designator parent)
        {
            if (Operator(OperatorType.LeftSquare) ||
                Operator(OperatorType.DiLeftSquare))
            {
                Expr expr = Ternary();
                if (expr == null)
                    Error("Expected expression after '['", ParseErrorID.DesignatorExpr);
                if (!Operator(OperatorType.RightSquare)
                    && !Operator(OperatorType.DiRightSquare))
                    Error("Expected ']' after expression", ParseErrorID.DesignatorRightSquare);
                if (expr == null)
                    return Designator(parent);
                return new ExprDesignator(parent, expr);
            }
            else if (Operator(OperatorType.Dot))
            {
                var i = Eat<IdentifierToken>();
                if (i == null)
                {
                    Error("Expected identifier after '.'", ParseErrorID.IdentifierAfterDot);
                    return Designator(parent);
                }
                return new MemberDesignator(parent, i.Value);
            }
            return null;
        }

        Designator DesignatorList()
        {
            Designator ret = null;
            for (;;)
            {
                var child = Designator(ret);
                if (child == null)
                    break;
                ret = child;
            }
            return ret;
        }

        Initializer InitializerList()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Advance();
            var initializers = new List<Initializer>();
            Token t = Current();
            do
            {
                (int, int) pos2 = Position;
                string fname2 = Filename;
                if (CheckOperator(OperatorType.DiRightBrace) || 
                    CheckOperator(OperatorType.RightBrace))
                    break;
                var designator = DesignatorList();
                if (designator != null && !Operator(OperatorType.Assign))
                    Error("Expected '=' after initializer designator", ParseErrorID.AssignAfterDesignator);
                Initializer initializer = null;
                if (CheckOperator(OperatorType.DiLeftBrace) || CheckOperator(OperatorType.LeftBrace))
                {
                    initializer = InitializerList();
                    initializer.Designator = designator;
                }
                else
                {
                    Expr expr = Assignment();
                    if (expr == null)
                        Error("Initializer expression can't be null", ParseErrorID.InitializerListExpression);
                    initializer = new ExprInitializer(pos2, fname2, designator, expr);
                }
                initializers.Add(initializer);
            }
            while (Operator(OperatorType.Comma));
            if (!Operator(OperatorType.RightBrace) && !Operator(OperatorType.DiRightBrace))
                Error("Expected '}' after initializer list", ParseErrorID.InitializerListRightBrace);
            return new ListInitializer(pos, fname, null, initializers);
        }

        Expr Primary()
        {
            var state = State;
            (int, int) pos = Position;
            string fname = Filename;
            Token t = Current();
            if (t = Eat<NumberToken>())
                return new LiteralExpr(pos, fname, t, (t as NumberToken).Parse(true, Warnings, Errors));
            else if (t = Eat<StringToken>())
            {
                StringToken concat = t as StringToken;
                if (concat.Char)
                    return new LiteralExpr(pos, fname, t, concat.CharToInteger(Warnings, Errors));
                StringToken t2;
                while (t2 = Eat<StringToken>())
                {
                    if (t2.Char)
                        Error("Can't concatenate character constant with string constant",
                            ParseErrorID.StrConcat);
                    else
                    {
                        concat.Value += t2.Value;
                        concat.Wide |= t2.Wide;
                    }
                }
                return new LiteralExpr(pos, fname, concat);
            }
            else if (Operator(OperatorType.LeftParen))
            {
                Token bb = Current();
                Expr expr = Expr();
                if (!Operator(OperatorType.RightParen))
                {
                    SetState(state);
                    return null;
                }
                return new GroupExpr(pos, fname, expr);
            }
            else if ((t = Check<IdentifierToken>()) && !Typedefs[t.Value])
            {
                Advance();
                return new IdentExpr(pos, fname, (t as IdentifierToken).Value);
            }
            return null;
        }

        Expr Postfix()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr expr = Primary();
            if (expr == null)
                return null;
            for (;;)
            {
                if (Operator(OperatorType.LeftSquare) || Operator(OperatorType.DiLeftSquare))
                {
                    Expr index = Expr();
                    if (index == null)
                        Error("Expected expression after '['", ParseErrorID.IndexExpr);
                    else
                    {
                        if (!Operator(OperatorType.RightSquare) && !Operator(OperatorType.DiRightSquare))
                            Error("Expected ']' after expression", ParseErrorID.IndexRightSquare);
                        expr = new IndexExpr(pos, fname, expr, index);
                    }
                }
                else if (Operator(OperatorType.LeftParen))
                {
                    List<Expr> args = new List<Expr>();
                    bool first = true;
                    do
                    {
                        Expr arg = Assignment();
                        if (expr == null && !first)
                            Error("Expected argument after ','", ParseErrorID.CallArgument);
                        if (expr != null)
                            args.Add(arg);
                        first = false;
                    }
                    while (Operator(OperatorType.Comma));
                    if (!Operator(OperatorType.RightParen))
                        Error("Expected ')' after call expression", ParseErrorID.CallRightParen);
                    expr = new CallExpr(pos, fname, expr, args);
                }
                else if (Operator(OperatorType.Dot))
                {
                    var name = Eat<IdentifierToken>();
                    if (name == null) Error("Expected member name after '.'", ParseErrorID.MemberNameExpected);
                    else expr = new DotExpr(pos, fname, expr, name.Value);
                }
                else if (Operator(OperatorType.PtrOp))
                {
                    var name = Eat<IdentifierToken>();
                    if (name == null) Error("Expected member name after '->'", ParseErrorID.MemberNameExpected);
                    else expr = new PtrExpr(pos, fname, expr, name.Value);
                }
                else if (Operator(OperatorType.IncOp))
                    expr = new PostIncExpr(pos, fname, expr);
                else if (Operator(OperatorType.DecOp))
                    expr = new PostDecExpr(pos, fname, expr);
                else
                    break;
            }
            return expr;
        }

        Expr Unary()
        {
            (int, int) pos = Position;
            string fname = Filename;
            if (Keyword(KeywordType.Sizeof))
            {
                var state = State;
                if (Operator(OperatorType.LeftParen))
                {
                    var ofType = TypeName();
                    if (ofType != null)
                    {
                        if (!Operator(OperatorType.RightParen))
                            Error("Expected ')' after type", ParseErrorID.TypeRightParen);
                        return new SizeofTypeExpr(pos, fname, ofType);
                    }
                    else
                    {
                        SetState(state);
                        Expr expr = Unary();
                        if (expr == null)
                        {
                            Error("Expected expression after 'sizeof'", ParseErrorID.SizeofExprExpected);
                            return null;
                        }
                        return new SizeofExpr(pos, fname, expr);
                    }
                }
            }

            var type = OperatorType.None;
            if ((type = Operator(new[] {
                    OperatorType.BitAnd,
                    OperatorType.Multiply,
                    OperatorType.Plus,
                    OperatorType.Minus,
                    OperatorType.BitNot,
                    OperatorType.Not,
                    OperatorType.IncOp,
                    OperatorType.DecOp,
                })) != OperatorType.None)
            {
                Expr unary = Cast();
                if (unary == null)
                    Error("Unexpected unary operator", ParseErrorID.UnexpectedToken);
                return new UnaryExpr(pos, fname, type, unary);
            }
            return Postfix();
        }

        Expr Cast()
        {
            (int, int) pos = Position;
            string fname = Filename;
            var state = State;
            if (Operator(OperatorType.LeftParen))
            {
                Token bb = Current();
                CType type = TypeName();
                if (type == null)
                    SetState(state);
                else
                {
                    if (!Operator(OperatorType.RightParen))
                        Error("Expected ')' after cast type", ParseErrorID.CastRightParen);
                    Expr expr = null;
                    if (CheckOperator(OperatorType.DiLeftBrace) ||
                        CheckOperator(OperatorType.LeftBrace))
                        expr = InitializerList();
                    else
                        expr = Cast();
                    if (expr != null)
                        return new CastExpr(pos, fname, expr, type);
                    else
                    {
                        Error("Expected expression after cast type", ParseErrorID.CastExprExpected);
                        return null;
                    }
                }
            }
            return Unary();
        }

        Expr Multiplication()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = Cast();
            var type = OperatorType.None;
            while ((type = Operator(
                new[] {
                    OperatorType.Multiply,
                    OperatorType.Divide,
                    OperatorType.Modulo
                })) != OperatorType.None)
            {

                if (left == null && type != OperatorType.Multiply)
                {
                    Error($"Unexpected '{(type == OperatorType.Divide ? "/" : "%")}'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = Cast();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, type, left, right);
            }
            return left;
        }

        Expr Addition()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = Multiplication();
            var type = OperatorType.None;
            if (left != null)
            {
                while ((type = Operator(new[] { OperatorType.Plus, OperatorType.Minus }))
                    != OperatorType.None)
                {
                    Expr right = Multiplication();
                    if (right == null)
                        Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                    else
                        left = new BinaryExpr(pos, fname, type, left, right);
                }
            }
            return left;
        }

        Expr Shift()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = Addition();
            var type = OperatorType.None;
            while ((type = Operator(new[] {
                OperatorType.LeftOp, OperatorType.RightOp }))
                != OperatorType.None)
            {
                if (left == null)
                {
                    Error($"Unexpected '{(type == OperatorType.LeftOp ? "<<" : ">>")}'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = Addition();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, type, left, right);
            }
            return left;
        }

        Expr Comparison()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = Shift();
            var type = OperatorType.None;
            while ((type = Operator(new[] {
                OperatorType.Less, OperatorType.LeOp,
                OperatorType.Greater, OperatorType.GeOp }))
                != OperatorType.None)
            {
                if (left == null)
                {
                    string op = "";
                    switch (type)
                    {
                        case OperatorType.Less: op = "<"; break;
                        case OperatorType.Greater: op = ">"; break;
                        case OperatorType.LeOp: op = "<="; break;
                        case OperatorType.GeOp: op = ">="; break;
                    }
                    Error($"Unexpected '{op}'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = Shift();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, type, left, right);
            }
            return left;
        }

        Expr Equality()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = Comparison();
            var type = OperatorType.None;
            while ((type = Operator(new[] { OperatorType.EqOp, OperatorType.NeOp }))
                != OperatorType.None)
            {
                if (left == null)
                {
                    Error($"Unexpected '{(type == OperatorType.EqOp ? "==" : "!=")}'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = Comparison();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, type, left, right);
            }
            return left;
        }

        Expr BitAND()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = Equality();
            if (left != null)
                while (Operator(OperatorType.BitAnd))
                {
                    Expr right = Equality();
                    if (right == null)
                        Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                    else
                        left = new BinaryExpr(pos, fname, OperatorType.BitAnd, left, right);
                }
            return left;
        }

        Expr BitXOR()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = BitAND();
            while (Operator(OperatorType.Xor))
            {
                if (left == null)
                {
                    Error("Unexpected '^'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = BitAND();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, OperatorType.Xor, left, right);
            }
            return left;
        }

        Expr BitOR()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = BitXOR();
            while (Operator(OperatorType.BitOr))
            {
                if (left == null)
                {
                    Error("Unexpected '|'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = BitXOR();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, OperatorType.BitOr, left, right);
            }
            return left;
        }

        Expr LogAND()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = BitOR();
            while (Operator(OperatorType.AndOp))
            {
                if (left == null)
                {
                    Error("Unexpected '&&'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = BitOR();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, OperatorType.AndOp, left, right);
            }
            return left;
        }

        Expr LogOR()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = LogAND();
            while (Operator(OperatorType.OrOp))
            {
                if (left == null)
                {
                    Error("Unexpected '||'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr right = LogAND();
                if (right == null)
                    Error("Expected right operand after operator", ParseErrorID.RightOperandExpected);
                else
                    left = new BinaryExpr(pos, fname, OperatorType.OrOp, left, right);
            }
            return left;
        }

        Expr Ternary()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr expr = LogOR();
            if (Operator(OperatorType.QuestionMark))
            {
                if (expr == null)
                {
                    Error("Unexpected '?'", ParseErrorID.UnexpectedToken);
                    Advance();
                    return null;
                }
                Expr @if = Expr();
                Expr @else = null;
                if (@if == null)
                    Error("Expected expression after '?'", ParseErrorID.TernaryQuestionMarkExpected);
                if (!Operator(OperatorType.Colon))
                    Error("Expected ':' in ternary expression", ParseErrorID.TernaryQuestionMarkExpected);
                else
                {
                    @else = Ternary();
                    if (@else == null)
                        Error("Expected expression after ':'", ParseErrorID.ExpressionAfterColon);
                }
                expr = new TernaryExpr(pos, fname, expr, @if, @else);
            }
            return expr;
        }

        Expr Assignment()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr left = Ternary();
            var type = OperatorType.None;
            if ((left is UnaryExpr || left is CallExpr ||
                 left is SizeofExpr || left is SizeofTypeExpr
                 || left is PostIncExpr || left is PostDecExpr
                 || left is CastExpr || left is PtrExpr
                 || left is DotExpr || left is IndexExpr
                 || left is GroupExpr || left is LiteralExpr
                 || left is IdentExpr)
                && (type = Operator(new[] {
                    OperatorType.Assign,
                    OperatorType.AddAssign,
                    OperatorType.SubAssign,
                    OperatorType.MulAssign,
                    OperatorType.DivAssign,
                    OperatorType.ModAssign,
                    OperatorType.LeftAssign,
                    OperatorType.RightAssign,
                    OperatorType.AndAssign,
                    OperatorType.XorAssign,
                    OperatorType.OrAssign
                })) != OperatorType.None)
            {
                Expr right = Assignment();
                if (right == null)
                    Error("Expected right operand after assignment operator", ParseErrorID.RightOperandExpected);
                else
                    left = new AssignExpr(pos, fname, type, left, right);
            }
            return left;
        }

        Expr Expr()
        {
            (int, int) pos = Position;
            string fname = Filename;
            Expr expr = Assignment();
            while (Operator(OperatorType.Comma))
            {
                Expr right = Assignment();
                if (right == null)
                    Error("Expected expression after ','", ParseErrorID.RightOperandExpected);
                else
                    expr = new BinaryExpr(pos, fname, OperatorType.Comma, expr, right);
            }
            return expr;
        }

        void Error(string msg, ParseErrorID id, int line = 0, int col = 0)
        {
            // Check for redundant errors
            if (id == _lastError && _tokensSinceLastError <= 2)
            {
                _tokensSinceLastError = 0;
                return;
            }
            if (line == 0) line = Position.Line;
            if (col == 0) col = Position.Column;
            Errors.Add(new Error(msg, Filename, line, col));
            _lastError = id;
            _tokensSinceLastError = 0;
        }
    }
}
