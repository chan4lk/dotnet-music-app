using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.AspNetCore.Components;

namespace Chinook.Pages;

/// <summary>
/// Used as the base class for pages with functionalities.
/// </summary>
public class ReactiveComponentBase : ComponentBase, IDisposable
{
    readonly Subject<Unit> parametersSet = new ();
    readonly Subject<Unit> disposed = new ();

    /// <summary>
    /// Track if page is disposed.
    /// </summary>
    protected IObservable<Unit> Disposed => disposed.AsObservable();
    
    /// <summary>
    /// Track the updated route params.
    /// </summary>
    protected IObservable<Unit> ParametersSet => parametersSet.AsObservable();
    
    /// <summary>
    /// Common message to be shown on top of the page.
    /// </summary>
    protected string? InfoMessage { get; set; }
    
    /// <summary>
    /// Stores if the message should show in red color.
    /// </summary>
    protected bool IsError { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        await base.SetParametersAsync(parameters);
        parametersSet.OnNext(Unit.Default); // Observable is updated so all the subscribers will trigger.
    }
    
    /// <summary>
    /// Close the info message and clear the status.
    /// </summary>
    protected void CloseInfoMessage()
    {
        IsError = false;
        InfoMessage = "";
    }

    /// <summary>
    /// Notify when the component disposes.
    /// All the subscription with TakeUntil(Disposed) with terminate.
    /// </summary>
    void IDisposable.Dispose()
    {
        disposed.OnNext(Unit.Default);
    }
}