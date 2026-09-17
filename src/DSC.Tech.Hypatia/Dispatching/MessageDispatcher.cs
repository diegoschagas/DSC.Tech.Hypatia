using System.Reflection;
using System.Runtime.ExceptionServices;
using DSC.Tech.Hypatia.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace DSC.Tech.Hypatia.Dispatching;

public sealed class MessageDispatcher(IServiceProvider serviceProvider) : IMessageDispatcher
{
    public Task Send<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        var messageType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(messageType);
        var handler = ResolveExactlyOne(handlerType, messageType);

        return Invoke<Task>(handler, handlerType, "Handle", command, cancellationToken);
    }

    public Task<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var messageType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(messageType, typeof(TResult));
        var handler = ResolveExactlyOne(handlerType, messageType);

        return Invoke<Task<TResult>>(handler, handlerType, "Handle", command, cancellationToken);
    }

    public Task<TResult> Query<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var messageType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(messageType, typeof(TResult));
        var handler = ResolveExactlyOne(handlerType, messageType);

        return Invoke<Task<TResult>>(handler, handlerType, "Handle", query, cancellationToken);
    }

    private object ResolveExactlyOne(Type handlerType, Type messageType)
    {
        var handlers = serviceProvider.GetServices(handlerType).Where(static handler => handler is not null).Cast<object>().ToArray();

        return handlers.Length switch
        {
            0 => throw new HandlerNotFoundException(messageType, handlerType),
            1 => handlers[0],
            _ => throw new MultipleHandlersException(messageType, handlerType, handlers.Length)
        };
    }

    private static TTask Invoke<TTask>(
        object handler,
        Type handlerType,
        string methodName,
        object message,
        CancellationToken cancellationToken)
        where TTask : Task
    {
        var method = handlerType.GetMethod(methodName)
            ?? throw new MissingMethodException(handlerType.FullName, methodName);

        try
        {
            return (TTask)(method.Invoke(handler, [message, cancellationToken])
                ?? throw new InvalidOperationException($"Handler '{handler.GetType().FullName}' returned null Task."));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
