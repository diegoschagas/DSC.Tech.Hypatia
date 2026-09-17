namespace DSC.Tech.Hypatia.Abstractions;

/// <summary>
/// Application boundary for dispatching commands and queries.
/// The dispatcher coordinates routing only; business behavior belongs to handlers.
/// </summary>
public interface IMessageDispatcher
{
    Task Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    Task<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);

    Task<TResult> Query<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default);
}
