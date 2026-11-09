using System.Reflection;
using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DSC.Tech.Hypatia.DependencyInjection;

/// <summary>Dependency injection integration for Hypatia.</summary>
public static class ServiceCollectionExtensions
{
    private static readonly Type[] HandlerDefinitions =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>)
    ];

    /// <summary>
    /// Registers <c>IMessageDispatcher</c> and every handler found in the given assemblies as scoped services.
    /// Calling it more than once with the same assemblies does not duplicate registrations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies scanned for handler implementations.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddHypatia(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        services.TryAddScoped<IMessageDispatcher, MessageDispatcher>();

        foreach (var assembly in assemblies.Distinct())
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (var implementationType in assembly.GetTypes()
                         .Where(type => type is { IsAbstract: false, IsInterface: false }))
            {
                foreach (var serviceType in implementationType.GetInterfaces().Where(IsHandlerInterface))
                {
                    services.TryAddEnumerable(
                        ServiceDescriptor.Scoped(serviceType, implementationType));
                }
            }
        }

        return services;
    }

    private static bool IsHandlerInterface(Type type) =>
        type.IsGenericType &&
        HandlerDefinitions.Contains(type.GetGenericTypeDefinition());
}
