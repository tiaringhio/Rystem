﻿using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class ServiceCollectionExtesions
    {
        internal static Dictionary<Type, Dictionary<Type, bool>> ScannedTypes { get; } = new();
        public static int Scan<T>(
            this IServiceCollection services,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
            => services.Scan(typeof(T), lifetime, assemblies);
        public static int Scan(
            this IServiceCollection services,
            Type serviceType,
            ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            var count = 0;
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes()
                    .Where(x => !x.IsInterface
                        && !x.IsAbstract))
                {
                    if ((!serviceType.IsInterface && type.IsTheSameTypeOrAFather(serviceType))
                        || (serviceType.IsInterface && type.HasInterface(serviceType)))
                    {
                        if (services.AddScannedType(serviceType, type, lifetime))
                            count++;
                    }
                }
            }
            return count;
        }
        public static int Scan(
           this IServiceCollection services,
           ServiceLifetime lifetime,
            params Assembly[] assemblies)
        {
            var count = 0;
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes().Where(x => !x.IsInterface && !x.IsAbstract))
                {
                    if (type.HasInterface(typeof(IScannable)))
                    {
                        var whatKindOfType = GetScannableInterfaceImplementation(type);
                        if (whatKindOfType != null
                            && services.AddScannedType(whatKindOfType, type, lifetime))
                            count++;
                    }
                }
            }
            return
                count;
        }
        private static bool AddScannedType(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime)
        {
            if (!ScannedTypes.ContainsKey(serviceType))
                ScannedTypes.TryAdd(serviceType, new());
            var scannedTypes = ScannedTypes[serviceType];
            if (!scannedTypes.ContainsKey(implementationType))
            {
                if (!(implementationType.HasInterface(serviceType) || implementationType.IsTheSameTypeOrAFather(serviceType)))
                    throw new ArgumentException($"It's not possible to assign {implementationType.FullName} to {serviceType.FullName} during scan setup of services.");
                if (implementationType.HasInterface(typeof(ITransientScannable)))
                    lifetime = ServiceLifetime.Transient;
                else if (implementationType.HasInterface(typeof(IScopedScannable)))
                    lifetime = ServiceLifetime.Scoped;
                else if (implementationType.HasInterface(typeof(ISingletonScannable)))
                    lifetime = ServiceLifetime.Singleton;

                if (serviceType == implementationType)
                {
                    if (!services.Any(x => x.ServiceType == serviceType))
                    {
                        services.AddService(serviceType, lifetime);
                        scannedTypes.TryAdd(implementationType, true);
                        return true;
                    }
                }
                else
                {
                    if (!services.Any(x => x.ServiceType == serviceType
                        && (x.ImplementationType == implementationType
                        || x.ImplementationInstance?.GetType() == implementationType)))
                    {
                        services.AddService(serviceType, implementationType, lifetime);
                        scannedTypes.TryAdd(implementationType, true);
                        return true;
                    }
                }
            }
            return false;
        }
        private static readonly Type s_objectType = typeof(object);
        private static Type? GetScannableInterfaceImplementation(Type? type)
        {
            if (type == s_objectType)
                return null;
            while (type != null && type != s_objectType)
            {
                var scannable = type.GetInterfaces().FirstOrDefault(
                    x => x.Name.StartsWith(nameof(IScannable)) && x.Name != nameof(IScannable));
                if (scannable != null)
                {
                    var genericType = scannable.GetGenericArguments()[0];
                    return genericType;
                }
                type = type.BaseType!;
            }
            return null;
        }
    }
}