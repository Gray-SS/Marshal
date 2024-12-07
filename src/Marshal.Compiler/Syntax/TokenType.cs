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
    Colon,

    // Keywords
    ProcKeyword,
    FuncKeyword,
    IntKeyword,
    
    //Specials
    EOF,
    Identifier,
    IntLiteral,
    StringLiteral,
    Invalid,
    Equal,
    SemiColon,
    ReturnKeyword,
    VarKeyword,
    ExternKeyword,
    ParamsKeyword,
    Comma,
}