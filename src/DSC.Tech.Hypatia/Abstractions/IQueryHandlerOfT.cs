namespace DSC.Tech.Hypatia.Abstractions;

/// <summary>Handles a query and returns data without changing application state.</summary>
/// <typeparam name="TQuery">The query type handled.</typeparam>
/// <typeparam name="TResult">The result type returned by the handler.</typeparam>
/// <remarks>Exactly one handler must be registered for each query type.</remarks>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>Executes the query and returns its result.</summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Token to cancel the operation; propagated by the dispatcher.</param>
    /// <returns>A task that produces the query result.</returns>
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
}
