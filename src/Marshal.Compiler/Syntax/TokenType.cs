namespace Marshal.Compiler.Syntax;

public enum TokenType : byte
{    
    // Characters
    Plus,
    Minus,
    Slash,
    Star,
    OpenBracket,
    CloseBracket,
    OpenCurlyBracket,
    CloseCurlyBracket,
    OpenSquareBracket,
    CloseSquareBracket,
    Equal,
    Comma,
    SemiColon,
    Colon,

    // Keywords
    ProcKeyword,
    FuncKeyword,
    ReturnKeyword,
    VarKeyword,
    ExternKeyword,
    ParamsKeyword,
    FalseKeyword,
    TrueKeyword,
    
    //Specials
    EOF,
    Identifier,
    IntLiteral,
    StringLiteral,
    CharLiteral,
    Invalid,
    NewKeyword,
    IfKeyword,
    ElseKeyword,
    EqualCond,
    BiggerThanEqCond,
    BiggerThanCond,
    LessThanEqCond,
    LessThanCond,
    Exclamation,
    NotEqualCond,
    Underscore,
    WhileKeyword,
    Percent,
    Ampersand,
    StructKeyword,
    FieldKeyword,
    Dot,
}