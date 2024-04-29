using Microsoft.AspNetCore.Components.Web;

namespace Chinook.Shared;

public partial class MainLayout
{
    private ErrorBoundary? errorBoundary;

    protected override void OnParametersSet()
    {
        errorBoundary?.Recover();
    }
}