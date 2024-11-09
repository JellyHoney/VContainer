using Microsoft.Extensions.DependencyInjection;
using System.Reflection.Metadata.Ecma335;
using VContainer.Internal;

namespace VContainer.Microsoft.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceProvider CreateVContainerServiceProvider(this IServiceCollection serviceCollection)
        {
            var container = new ContainerBuilder();
            return container.BuildServiceProvider(serviceCollection);
        }
    }

    public static class ContainerBuilderExtensions
    {
        public static IServiceProvider BuildServiceProvider(this ContainerBuilder builder, IServiceCollection serviceCollection)
        {
            builder.Register<IServiceProvider>(resolver => new VContainerServiceProvider(resolver), Lifetime.Scoped);
            builder.Register<IServiceScopeFactory>(resolver => new VContainerScopedFactory(resolver), Lifetime.Singleton);
            builder.Register<IServiceProviderIsService, VContainerIServiceProviderIsService>(Lifetime.Scoped);

            foreach (var service in serviceCollection)
            {
                builder.AddServices(service);
            }

            var resolver = builder.Build();
            return new VContainerServiceProvider(resolver);
        }

        private static Lifetime Convert2VContainerLifetime(ServiceLifetime lifetime)
        {
            return lifetime switch
            {
                ServiceLifetime.Singleton => Lifetime.Singleton,
                ServiceLifetime.Scoped => Lifetime.Scoped,
                ServiceLifetime.Transient => Lifetime.Transient,
                _ => throw new NotImplementedException(),
            };
        }

        private static void AddServices(this ContainerBuilder builder, ServiceDescriptor serviceDescriptor)
        {
            var lifetime = Convert2VContainerLifetime(serviceDescriptor.Lifetime);
            if (serviceDescriptor.ImplementationInstance != null)
            {
                var registraction = new InstanceRegistrationBuilder(serviceDescriptor.ImplementationInstance);
                builder.Register(registraction);
            }
            else if (serviceDescriptor.ImplementationFactory != null)
            {
                var registration = new FuncRegistrationBuilder(resolver =>
                {
                    var provider = resolver.Resolve<IServiceProvider>();
                    return serviceDescriptor.ImplementationFactory(provider);
                }, serviceDescriptor.ServiceType, lifetime);
                builder.Register(registration);
            }
            else
            {
                builder.Register(serviceDescriptor.ServiceType, serviceDescriptor.ImplementationType, lifetime);
            }
        }
    }

    public class VContainerServiceProvider : IServiceProvider, ISupportRequiredService, IDisposable
    {
        private readonly IObjectResolver mObjectResolver;

        public VContainerServiceProvider(IObjectResolver objectResolver)
        {
            mObjectResolver = objectResolver;
        }

        public void Dispose()
        {
            mObjectResolver.Dispose();
        }

        public object GetRequiredService(Type serviceType)
        {
            return mObjectResolver.Resolve(serviceType);
        }

        public object? GetService(Type serviceType)
        {
            mObjectResolver.TryResolve(serviceType, out var ret);
            return ret;
        }
    }

    public class VContainerScopedFactory(IObjectResolver objectResolver) : IServiceScopeFactory
    {
        private readonly IObjectResolver mObjectResolver = objectResolver;

        public IServiceScope CreateScope()
        {
            return new VContainerScope(mObjectResolver.CreateScope());
        }
    }

    public class VContainerScope : IServiceScope
    {
        private VContainerServiceProvider mServiceProvider;

        public VContainerScope(IObjectResolver mObjectResolver)
        {
            mServiceProvider = new VContainerServiceProvider(mObjectResolver);
        }

        public IServiceProvider ServiceProvider => mServiceProvider;

        public void Dispose()
        {
            mServiceProvider.Dispose();
        }
    }

    public class VContainerIServiceProviderIsService : IServiceProviderIsService
    {
        private readonly IObjectResolver mObjectResolver;

        public VContainerIServiceProviderIsService(IObjectResolver objectResolver)
        {
            this.mObjectResolver = objectResolver;
        }

        public bool IsService(Type serviceType)
        {
            return mObjectResolver.TryResolve(serviceType, out var _);
        }
    }
}
