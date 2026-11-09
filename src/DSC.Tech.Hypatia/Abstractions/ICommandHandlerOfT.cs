namespace DSC.Tech.Hypatia.Abstractions;

/// <summary>Handles a command that returns a result.</summary>
/// <typeparam name="TCommand">The command type handled.</typeparam>
/// <typeparam name="TResult">The result type returned by the handler.</typeparam>
/// <remarks>Exactly one handler must be registered for each command type.</remarks>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Executes the use case for the command and returns its result.</summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation; propagated by the dispatcher.</param>
    /// <returns>A task that produces the command result.</returns>
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
}
