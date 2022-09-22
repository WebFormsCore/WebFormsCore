using WebForms.Models;

namespace WebForms.Nodes;

public interface IAttributeNode
{
    Dictionary<TokenString, TokenString> Attributes { get; }
}
