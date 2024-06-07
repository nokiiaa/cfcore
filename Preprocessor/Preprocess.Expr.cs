using System;
using cfcore.Parser.AST;

namespace cfcore.Preprocessor
{
    public partial class Preprocess
    {
        internal abstract class Expr
        {
            public (int Line, int Column) Position { get; set; }
            public Expr(int line, int col)
                => Position = (line, col);
        }

        internal class NameExpr : Expr
        {
            public string Name { get; set; }
            public NameExpr(int line, int col, string name)
                : base(line, col) => Name = name;
        }

        internal class LiteralExpr : Expr
        {
            public ulong Value { get; set; }
            public LiteralExpr(int line, int col, ulong value)
                : base(line, col) => Value = value;
        }

        internal class BinaryExpr : Expr
        {
            public Expr Left { get; set; }
            public OperatorType Op { get; set; }
            public Expr Right { get; set; }

            public BinaryExpr(int line, int col, Expr left, OperatorType op, Expr right)
                : base(line, col)
            {
                Left = left;
                Op = op;
                Right = right;
            }
        }

        internal class UnaryExpr : Expr
        {
            public OperatorType Op { get; set; }
            public Expr Expr { get; set; }

            public UnaryExpr(int line, int col, OperatorType op, Expr expr)
                : base(line, col)
            {
                Expr = expr;
                Op = op;
            }
        }

        internal class TernaryExpr : Expr
        {
            public Expr Condition { get; set; }
            public Expr If { get; set; }
            public Expr Else { get; set; }

            public TernaryExpr(int line, int col, Expr condition, Expr @if, Expr @else)
                : base(line, col)
            {
                Condition = condition;
                If = @if;
                Else = @else;
            }
        }

        ulong ParseAndEvaluate()
        {
            Expr Primary()
            {
                Token t = null;
                if (t = TokenEat<NumberToken>())
                {
                    var nl = (t as NumberToken).Parse(false, Warnings, Errors);
                    if (nl is FloatLiteral)
                        Error("Floating-point literals not allowed in preprocessor expressions");
                    return new LiteralExpr(t.Position.Line, t.Position.Column,
                        nl is IntLiteral il ? il.Value : 0ul);
                }
                else if (t = TokenEat<StringToken>())
                    return new LiteralExpr(t.Position.Line, t.Position.Column,
                        (t as StringToken).CharToInteger(Warnings, Errors).Value);
                else if (t = OperatorEat(OperatorType.LeftParen))
                {
                    Expr expr = Top();
                    if (expr == null)
                        Error("Expected '(' expr ')'");
                    if (!OperatorEat(OperatorType.RightParen))
                        Error("Expected ')' after expression");
                    return expr;
                }
                else if (t = TokenEat<IdentifierToken>())
                    return new NameExpr(t.Position.Line, t.Position.Column, t.Value);
                else
                {
                    Error($"Unexpected token '{TokenAdvance().Value}'");
                    return null;
                }
            }

            Expr Unary()
            {
                OperatorToken t;
                if ((t = OperatorEat(OperatorType.Plus)) || (t = OperatorEat(OperatorType.Minus)) ||
                    (t = OperatorEat(OperatorType.BitNot)) || (t = OperatorEat(OperatorType.Not)))
                {
                    Expr expr = Unary();
                    if (expr == null)
                    {
                        Error($"Expected expression after '{t.Value}'");
                        return null;
                    }
                    return new UnaryExpr(t.Position.Line, t.Position.Column, t.Type, expr);
                }
                return Primary();
            }

            Expr Multiplication()
            {
                Expr left = Unary();
                OperatorToken t;
                while ((t = OperatorEat(OperatorType.Multiply)) ||
                    (t = OperatorEat(OperatorType.Divide))
                    || (t = OperatorEat(OperatorType.Modulo)))
                {
                    Expr right = Unary();
                    if (left == null)
                    {
                        Error("Expected expression before ','");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error("Expected expression after ','");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            t.Type, right);
                }
                return left;
            }

            Expr Addition()
            {
                Expr left = Multiplication();
                OperatorToken t;
                while ((t = OperatorEat(OperatorType.Plus)) || 
                    (t = OperatorEat(OperatorType.Minus)))
                {
                    Expr right = Multiplication();
                    if (right == null)
                        Error($"Expected expression after '{t.Value}'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            t.Type, right);
                }
                return left;
            }

            Expr Shift()
            {
                Expr left = Addition();
                OperatorToken t;
                while ((t = OperatorEat(OperatorType.LeftOp)) || 
                    (t = OperatorEat(OperatorType.RightOp)))
                {
                    Expr right = Addition();
                    if (left == null)
                    {
                        Error($"Expected expression before '{t.Value}'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error($"Expected expression after '{t.Value}'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            t.Type, right);
                }
                return left;
            }


            Expr Comparison()
            {
                Expr left = Shift();
                OperatorToken t;
                while ((t = OperatorEat(OperatorType.Less)) || (t = OperatorEat(OperatorType.Greater))
                    || (t = OperatorEat(OperatorType.LeOp)) || (t = OperatorEat(OperatorType.GeOp)))
                {
                    Expr right = Shift();
                    if (left == null)
                    {
                        Error($"Expected expression before '{t.Value}'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error($"Expected expression after '{t.Value}'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            t.Type, right);
                }
                return left;
            }

            Expr Equality()
            {
                Expr left = Comparison();
                OperatorToken t;
                while ((t = OperatorEat(OperatorType.EqOp)) || (t = OperatorEat(OperatorType.NeOp)))
                {
                    Expr right = Comparison();
                    if (left == null)
                    {
                        Error($"Expected expression before '{t.Value}'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error($"Expected expression after '{t.Value}'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            t.Type, right);
                }
                return left;
            }

            Expr BitAND()
            {
                Expr left = Equality();
                while (OperatorEat(OperatorType.BitAnd))
                {
                    Expr right = Equality();
                    if (left == null)
                    {
                        Error($"Expected expression before '&'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error($"Expected expression after '&'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            OperatorType.BitAnd, right);
                }
                return left;
            }

            Expr XOR()
            {
                Expr left = BitAND();
                while (OperatorEat(OperatorType.Xor))
                {
                    Expr right = BitAND();
                    if (left == null)
                    {
                        Error($"Expected expression before '^'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error($"Expected expression after '^'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            OperatorType.Xor, right);
                }
                return left;
            }


            Expr BitOR()
            {
                Expr left = XOR();
                while (OperatorEat(OperatorType.BitOr))
                {
                    Expr right = XOR();
                    if (left == null)
                    {
                        Error("Expected expression before '|'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error("Expected expression after '|'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            OperatorType.BitOr, right);
                }
                return left;
            }

            Expr LogAND()
            {
                Expr left = BitOR();
                while (OperatorEat(OperatorType.AndOp))
                {
                    Expr right = BitOR();
                    if (left == null)
                    {
                        Error("Expected expression before '&&'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error("Expected expression after '&&'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            OperatorType.AndOp, right);
                }
                return left;
            }


            Expr LogOR()
            {
                Expr left = LogAND();
                while (OperatorEat(OperatorType.OrOp))
                {
                    Expr right = LogAND();
                    if (left == null)
                    {
                        Error("Expected expression before '||'");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error("Expected expression after '||'");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            OperatorType.OrOp, right);
                }
                return left;
            }

            Expr Ternary()
            {
                Expr cond = LogOR();
                if (OperatorEat(OperatorType.QuestionMark))
                {
                    Expr @if = Top();
                    if (@if == null)
                        Error("Expected 'if' expression of conditional expression");
                    if (!OperatorEat(OperatorType.Colon))
                        Error("Expected ':'");
                    Expr @else = Ternary();
                    if (@else == null)
                        Error("Expected 'if' expression of conditional expression");
                    if (@if == null && @else == null)
                        return cond;
                    cond = new TernaryExpr(cond.Position.Line, cond.Position.Column, cond, @if, @else);
                }
                return cond;
            }

            Expr Top()
            {
                Expr left = Ternary();
                while (OperatorEat(OperatorType.Comma))
                {
                    Expr right = Ternary();
                    if (left == null)
                    {
                        Error("Expected expression before ','");
                        TokenAdvance();
                        return null;
                    }
                    if (right == null)
                        Error("Expected expression after ','");
                    else
                        left = new BinaryExpr(left.Position.Line, left.Position.Column, left,
                            OperatorType.Comma, right);
                }
                return left;
            }

            ulong Eval(Expr expr)
            {
                ulong val = 0;
                switch (expr)
                {
                    case BinaryExpr be:
                        switch (be.Op)
                        {
                            case OperatorType.Plus: return Eval(be.Left) + Eval(be.Right);
                            case OperatorType.Minus: return Eval(be.Left) - Eval(be.Right);
                            case OperatorType.Multiply: return Eval(be.Left) * Eval(be.Right);
                            case OperatorType.Divide:
                                val = 0;
                                try
                                {
                                    val = (ulong)((long)Eval(be.Left) / (long)Eval(be.Right));
                                }
                                catch (DivideByZeroException)
                                {
                                    Error("Cannot divide by zero", 
                                        be.Right.Position.Line, be.Right.Position.Column);
                                }
                                return val;
                            case OperatorType.Modulo:
                                val = 0;
                                try
                                {
                                    val = (ulong)((long)Eval(be.Left) % (long)Eval(be.Right));
                                }
                                catch (DivideByZeroException)
                                {
                                    Error("Cannot take remainder of division by zero",
                                        be.Right.Position.Line, be.Right.Position.Column);
                                }
                                return val;
                            case OperatorType.Comma:
                                Eval(be.Left);
                                return Eval(be.Right);
                            case OperatorType.AndOp:
                                return (Eval(be.Left) != 0ul && Eval(be.Right) != 0ul) ? 1ul : 0ul;
                            case OperatorType.OrOp:
                                return (Eval(be.Left) != 0ul || Eval(be.Right) != 0ul) ? 1ul : 0ul;
                            case OperatorType.BitAnd:
                                return Eval(be.Left) & Eval(be.Right);
                            case OperatorType.BitOr:
                                return Eval(be.Left) | Eval(be.Right);
                            case OperatorType.Xor:
                                return Eval(be.Left) ^ Eval(be.Right);
                            case OperatorType.EqOp:
                                return (Eval(be.Left) == Eval(be.Right)) ? 1ul : 0ul;
                            case OperatorType.NeOp:
                                return (Eval(be.Left) == Eval(be.Right)) ? 0ul : 1ul;
                            case OperatorType.LeOp:
                                return (Eval(be.Left) <= Eval(be.Right)) ? 1ul : 0ul;
                            case OperatorType.GeOp:
                                return (Eval(be.Left) >= Eval(be.Right)) ? 1ul : 0ul;
                            case OperatorType.Less:
                                return (Eval(be.Left) < Eval(be.Right)) ? 1ul : 0ul;
                            case OperatorType.Greater:
                                return (Eval(be.Left) > Eval(be.Right)) ? 1ul : 0ul;
                            case OperatorType.LeftOp:
                                return Eval(be.Left) << (int)Eval(be.Right);
                            case OperatorType.RightOp:
                                return Eval(be.Left) >> (int)Eval(be.Right);
                            default:
                                // Should never happen anyway
                                Error($"Unknown operation type {be.Op}", be.Position.Line, be.Position.Column);
                                return 0;
                        }
                    case UnaryExpr ue:
                        switch (ue.Op)
                        {
                            case OperatorType.Plus: return Eval(ue.Expr);
                            case OperatorType.Minus: return (ulong)-(long)Eval(ue.Expr);
                            case OperatorType.BitNot: return ~Eval(ue.Expr);
                            case OperatorType.Not: return Eval(ue.Expr) == 0ul ? 1ul : 0ul;
                            default:
                                // Should never happen anyway
                                Error($"Unknown operation type {ue.Op}", ue.Position.Line, ue.Position.Column);
                                return 0ul;
                        }
                    case TernaryExpr te:
                        return Eval(te.Condition) == 0 ? Eval(te.Else) : Eval(te.If);
                    case LiteralExpr le:
                        return le.Value;
                    case NameExpr ne:
                    default:
                        return 0ul;
                }
            }

            Expr expr = Top();
            if (expr == null)
                Error("Expected condition");
            return Eval(expr);
        }
    }
}
