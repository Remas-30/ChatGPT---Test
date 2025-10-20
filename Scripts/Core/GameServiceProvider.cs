using System;
using System.Collections.Generic;

namespace GalacticExpansion.Core
{
    /// <summary>
    /// Simple dictionary-based service provider for wiring runtime systems.
    /// </summary>
    public sealed class GameServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> _services = new();

        public void Register<T>(T instance) where T : class
        {
            _services[typeof(T)] = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public object? GetService(Type serviceType)
        {
            return _services.TryGetValue(serviceType, out object? service) ? service : null;
        }
    }
}
