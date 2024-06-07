using System.Collections.Generic;

namespace cfcore.Parser.AST
{
    public abstract class Declarator
    {
        public abstract CType ToType(CType baseType);

        public abstract string GetName();
    }

    public class NameDeclarator : Declarator
    {
        public string Name { get; set; }

        public NameDeclarator(string name)
            => Name = name;

        public override CType ToType(CType baseType) => baseType;
        public override string GetName() => Name;
    }

    public class PtrDeclarator : Declarator
    {
        public CTypeQualifiers Qualifiers { get; set; }
        public Declarator Parent { get; set; }

        public PtrDeclarator(Declarator parent,
            CTypeQualifiers qualifiers = CTypeQualifiers.None)
        {
            Parent = parent;
            Qualifiers = qualifiers;
        }

        public override CType ToType(CType baseType)
        {
            CType type = new CPointerType(Qualifiers, baseType);
            return Parent != null ? Parent.ToType(type) : type;
        }
        public override string GetName() => Parent == null ? null : Parent.GetName();
    }
    public class ArrayDeclarator : Declarator
    {
        public bool Static { get; set; }
        public CTypeQualifiers Qualifiers { get; set; }
        public Declarator Parent { get; set; }
        public bool VlaAsterisk { get; set; }
        public Expr Length { get; set; }

        public ArrayDeclarator(Expr length, 
            CTypeQualifiers qualifiers = CTypeQualifiers.None,
            bool @static = false, 
            Declarator parent = null)
        {
            Qualifiers = qualifiers;
            Static = @static;
            Parent = parent;
            Length = length;
        }
        public ArrayDeclarator(Declarator parent = null,
            CTypeQualifiers qualifiers = CTypeQualifiers.None, 
            bool @static = false, 
            bool vlaAsterisk = false)
        {
            Parent = parent;
            Qualifiers = qualifiers;
            Static = @static;
            VlaAsterisk = vlaAsterisk;
        }

        public override CType ToType(CType baseType)
        {
            if (Parent is ArrayDeclarator || Parent is FuncDeclarator || Parent is GroupDeclarator)
            {
                CType arr;
                if (VlaAsterisk)
                    arr = new CArrayType(Qualifiers, baseType, true, Static);
                else
                    arr = new CArrayType(Qualifiers, baseType, Length, Static);
                return Parent.ToType(arr);
            }
            CType from = Parent == null ? baseType : Parent.ToType(baseType);
            if (VlaAsterisk)
                from = new CArrayType(Qualifiers, from, true, Static);
            else
                from = new CArrayType(Qualifiers, from, Length, Static);
            return from;
        }

        public override string GetName() => Parent == null ? null : Parent.GetName();
    }

    public class FuncDeclarator : Declarator
    {
        public Declarator Parent { get; set; }
        public List<(CType, string)> Arguments { get; set; } 
            = new List<(CType, string)>();

        public FuncDeclarator(Declarator parent = null, List<(CType, string)> args = null)
        {
            Parent = parent;
            if (args != null) Arguments = args;
        }
        public override CType ToType(CType baseType)
        {
            if (Parent is ArrayDeclarator || Parent is FuncDeclarator || Parent is GroupDeclarator)
                return Parent.ToType(new CFunctionType(baseType, Arguments));
            CType from = Parent == null ? baseType : Parent.ToType(baseType);
            return new CFunctionType(from, Arguments);
        }
        public override string GetName() => Parent == null ? null : Parent.GetName();
    }


    public class GroupDeclarator : Declarator
    {
        public Declarator Declarator { get; set; }
        public GroupDeclarator(Declarator decl)
            => Declarator = decl;

        public override CType ToType(CType baseType)
            => Declarator.ToType(baseType);

        public override string GetName() => Declarator.GetName();
    }
}
