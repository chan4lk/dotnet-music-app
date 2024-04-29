using Microsoft.AspNetCore.Components;

namespace Chinook.Shared.Components;

public partial class InfoMessage : ComponentBase
{
    [Parameter]
    public string? Message { get; set; } 
    
    [Parameter]
    public bool IsError { get; set; }

    private void CloseInfoMessage()
    {
        Message = "";
        IsError = false;
    }
}