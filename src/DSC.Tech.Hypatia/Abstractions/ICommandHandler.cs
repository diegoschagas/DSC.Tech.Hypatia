namespace DSC.Tech.Hypatia.Abstractions;

/// <summary>Handles a command that does not return a value.</summary>
/// <typeparam name="TCommand">The command type handled.</typeparam>
/// <remarks>Exactly one handler must be registered for each command type.</remarks>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Executes the use case for the command.</summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation; propagated by the dispatcher.</param>
    /// <returns>A task that completes when the command has been handled.</returns>
    Task Handle(TCommand command, CancellationToken cancellationToken = default);
}
