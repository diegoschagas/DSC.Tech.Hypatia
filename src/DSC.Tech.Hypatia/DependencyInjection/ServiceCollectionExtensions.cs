using System.Reflection;
using DSC.Tech.Hypatia.Abstractions;
using DSC.Tech.Hypatia.Dispatching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DSC.Tech.Hypatia.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private static readonly Type[] HandlerDefinitions =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>)
    ];

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
