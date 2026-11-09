namespace DSC.Tech.Hypatia.Abstractions;

/// <summary>
/// Application boundary for dispatching commands and queries.
/// The dispatcher coordinates routing only; business behavior belongs to handlers.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>Sends a command that does not return a value to its single handler.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to send. Must not be null.</param>
    /// <param name="cancellationToken">Token propagated to the handler.</param>
    /// <returns>A task that completes when the handler finishes.</returns>
    /// <exception cref="DSC.Tech.Hypatia.Dispatching.HandlerNotFoundException">No handler is registered for the message type.</exception>
    /// <exception cref="DSC.Tech.Hypatia.Dispatching.MultipleHandlersException">More than one handler is registered for the message type.</exception>
    Task Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>Sends a command that returns a result to its single handler.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="command">The command to send. Must not be null.</param>
    /// <param name="cancellationToken">Token propagated to the handler.</param>
    /// <returns>A task that produces the handler result.</returns>
    /// <exception cref="DSC.Tech.Hypatia.Dispatching.HandlerNotFoundException">No handler is registered for the message type.</exception>
    /// <exception cref="DSC.Tech.Hypatia.Dispatching.MultipleHandlersException">More than one handler is registered for the message type.</exception>
    Task<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);

    /// <summary>Sends a query to its single handler and returns the data.</summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="query">The query to execute. Must not be null.</param>
    /// <param name="cancellationToken">Token propagated to the handler.</param>
    /// <returns>A task that produces the query result.</returns>
    /// <exception cref="DSC.Tech.Hypatia.Dispatching.HandlerNotFoundException">No handler is registered for the message type.</exception>
    /// <exception cref="DSC.Tech.Hypatia.Dispatching.MultipleHandlersException">More than one handler is registered for the message type.</exception>
    Task<TResult> Query<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default);
}
