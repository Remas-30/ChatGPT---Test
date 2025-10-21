using System;

namespace GalacticExpansion.Core
{
    /// <summary>
    /// Basic service locator for runtime-only interactions. Avoid for gameplay logic where dependency injection is feasible.
    /// </summary>
    public static class ServiceLocator
    {
        private static IServiceProvider? _provider;

        public static void Provide(IServiceProvider provider)
        {
            _provider = provider;
        }

        public static T Get<T>() where T : class
        {
            if (_provider == null)
            {
                throw new InvalidOperationException("Service provider has not been configured.");
            }

            T? service = _provider.GetService(typeof(T)) as T;
            if (service == null)
            {
                throw new InvalidOperationException($"Requested service of type {typeof(T).Name} was not found.");
            }

            return service;
        }
    }
}
