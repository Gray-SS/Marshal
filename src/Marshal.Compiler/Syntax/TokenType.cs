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
    Colon,

    // Keywords
    ProcKeyword,
    FuncKeyword,
    IntKeyword,
    
    //Specials
    EOF,
    Identifier,
    NumberLiteral,
    Invalid,
    Equal,
    SemiColon,
    ReturnKeyword,
    VarKeyword,
    ExternKeyword,
    ParamsKeyword,
    Comma,
}