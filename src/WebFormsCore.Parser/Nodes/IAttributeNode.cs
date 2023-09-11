using WebFormsCore.Models;

namespace WebFormsCore.Nodes;

public interface IAttributeNode
{
    Dictionary<TokenString, AttributeValue> Attributes { get; }
}
