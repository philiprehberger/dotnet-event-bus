using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Philiprehberger.EventBus;

/// <summary>
/// Extension methods for registering the event bus with Microsoft dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IEventBus"/> as a singleton and wires up all <see cref="IEventHandler{T}"/>
    /// implementations found in the calling assembly.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="configure">Optional action to configure <see cref="EventBusOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Action<EventBusOptions>? configure = null)
    {
        return AddEventBus(services, Assembly.GetCallingAssembly(), configure);
    }

    /// <summary>
    /// Registers <see cref="IEventBus"/> as a singleton and wires up all <see cref="IEventHandler{T}"/>
    /// implementations found in the specified assembly.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <param name="assembly">The assembly to scan for handler implementations.</param>
    /// <param name="configure">Optional action to configure <see cref="EventBusOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventBus(
        this IServiceCollection services,
        Assembly assembly,
        Action<EventBusOptions>? configure = null)
    {
        var options = new EventBusOptions();
        configure?.Invoke(options);

        services.AddSingleton<IEventBus>(new EventBus(options));

        var handlerInterfaceType = typeof(IEventHandler<>);

        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType));

        foreach (var handlerType in handlerTypes)
        {
            var interfaces = handlerType.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterfaceType);

            foreach (var @interface in interfaces)
            {
                services.AddTransient(@interface, handlerType);
            }
        }

        return services;
    }
}
