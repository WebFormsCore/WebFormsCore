namespace WebFormsCore.Models;

public enum TokenType
{
    None,

    StartDirective,
    EndDirective,

    Expression,
    EncodeExpression,
    EvalExpression,
    Statement,

    ServerComment,

    ElementNamespace,
    ElementName,

    TagOpen,
    TagOpenSlash,
    TagClose,
    TagSlashClose,

    DocType,
    Comment,
    Text,

    Attribute,
    AttributeValue
}
