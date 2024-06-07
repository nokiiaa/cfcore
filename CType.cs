using cfcore.Parser.AST;
using System;
using System.Collections.Generic;

namespace cfcore
{
    [Flags]
    public enum CTypeQualifiers
    {
        None = 0,
        Const = 1,
        Volatile = 2,
        Restrict = 4
    }

    [Flags]
    public enum CPrimitiveTypeSpecifiers
    {
        None = 0,
        Void = 1,
        Char = 2,
        Short = 4,
        Int = 8,
        Long = 16,
        LongLong = 32,
        Float = 64,
        Double = 128,
        Signed = 256,
        Unsigned = 512,
        Bool = 1024,
        Complex = 2048,
        Imaginary = 4096
    }

    public class CType
    {
        public CTypeQualifiers Qualifiers { get; set; }
        public CType(CTypeQualifiers qualifiers) 
            => Qualifiers = qualifiers;
    }
    public class CNamedType : CType
    {
        public string Name { get; set; }
        public CNamedType(CTypeQualifiers qualifiers,
            string name) : base(qualifiers)
            => Name = name;
    }

    public class CPrimitiveType : CType
    {
        public CPrimitiveTypeSpecifiers Specifiers { get; set; }
        public CPrimitiveType(CTypeQualifiers qualifiers,
            CPrimitiveTypeSpecifiers specifiers) : base(qualifiers)
            => Specifiers = specifiers;
    }

    public class CPointerType : CType
    {
        public CType To { get; set; }
        public CPointerType(CTypeQualifiers qualifiers, CType to)
            : base(qualifiers)
            => To = to;
    }

    public class CArrayType : CType
    {
        // C99 VLA declaration (int [*])
        public bool VlaAsterisk { get; set; }
        public Expr Length { get; set; }
        public CType Of { get; set; }

        public bool Static { get; set; }

        public CArrayType(CTypeQualifiers qualifiers, CType of,
            Expr length, bool @static = false)
            : base(qualifiers)
        {
            Static = @static;
            Of = of;
            Length = length;
            VlaAsterisk = false;
        }

        public CArrayType(CTypeQualifiers qualifiers, CType of,
            bool vlaAsterisk = false, bool @static = false) : base(qualifiers)
        {
            Static = @static;
            Of = of;
            VlaAsterisk = vlaAsterisk;
        }
    }

    public class CStructUnionType : CType
    {
        public bool Union { get; set; }
        public bool NameOnly { get; set; }
        public string Name { get; set; }
        public List<StructUnionDecl> Members { get; set; } 
            = new List<StructUnionDecl>();

        public CStructUnionType(CTypeQualifiers qualifiers,
            bool union,
            List<StructUnionDecl> members = null,
            string name = "")
            : base(qualifiers)
        {
            if (members != null) Members = members;
            Name = name;
            Union = union;
        }

        public CStructUnionType(CTypeQualifiers qualifiers,
            bool union, string name)
            : base(qualifiers)
        {
            NameOnly = true;
            Union = union;
            Name = name;
        }
    }

    public class CEnumType : CType
    {
        public bool NameOnly { get; set; }
        public string Name { get; set; }
        public List<(string, Expr)> Constants { get; set; }
            = new List<(string, Expr)>();

        public CEnumType(CTypeQualifiers qualifiers,
            List<(string, Expr)> constants = null,
            string name = "")
            : base(qualifiers)
        {
            if (constants != null) Constants = constants;
            Name = name;
        }

        public CEnumType(CTypeQualifiers qualifiers, string name)
            : base(qualifiers)
        {
            NameOnly = true;
            Name = name;
        }
    }

    public class CFunctionType : CType
    {
        public CType ReturnType { get; set; }
        public List<(CType, string)> ArgumentList { get; set; } = new List<(CType, string)>();

        public CFunctionType(CType ret, List<(CType, string)> arglist = null)
            : base(CTypeQualifiers.None)
        {
            ReturnType = ret;
            if (arglist != null)
                ArgumentList = arglist;
        }
    }
}
