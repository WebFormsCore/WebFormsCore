using System.Threading.Tasks;
using WebFormsCore.UI;

namespace WebFormsCore.Events;

public interface IPageService
{
    Task BeforeInitializeAsync(Page page);

    Task AfterInitializeAsync(Page page);
}
