using WebFormsCore.UI.WebControls;

namespace WebFormsCore.UI.Attributes;

public class UnitAttributeParser : IAttributeParser<Unit>
{
    public Unit Parse(string value)
    {
        return Unit.Parse(value);
    }
}
