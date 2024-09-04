using System.Threading.Tasks;

namespace WebFormsCore;

public interface ITestContext
{
    Task ReloadAsync();

    IElement GetElementById(string id);
}