namespace Marshal.Compiler.Syntax;

public enum TokenType : byte
{    
    // Characters
    Plus,
    Minus,
    Slash,
    Asterisk,
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
    ExclamationMark,
    NotEqualCond,
    Underscore,
}