using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using Splat;

namespace PodBoy.Bootstrap
{
    public class AutofacDependencyResolver : IMutableDependencyResolver
    {
        private readonly IContainer container;

        public AutofacDependencyResolver(IContainer container)
        {
            this.container = container;
        }

        public void Dispose()
        {
            container.Dispose();
        }

        public object GetService(Type serviceType, string contract = null)
        {
            try
            {
                return string.IsNullOrEmpty(contract)
                    ? container.Resolve(serviceType)
                    : container.ResolveNamed(contract, serviceType);
            }
            catch (DependencyResolutionException)
            {
                return null;
            }
        }

        public IEnumerable<object> GetServices(Type serviceType, string contract = null)
        {
            try
            {
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(serviceType);
                object instance = string.IsNullOrEmpty(contract)
                    ? container.Resolve(enumerableType)
                    : container.ResolveNamed(contract, enumerableType);
                return ((IEnumerable) instance).Cast<object>();
            }
            catch (DependencyResolutionException)
            {
                return null;
            }
        }

        public void Register(Func<object> factory, Type serviceType, string contract = null)
        {
            container.BeginLifetimeScope(builder =>
            {
                if (string.IsNullOrEmpty(contract))
                {
                    builder.Register(x => factory()).As(serviceType).AsImplementedInterfaces();
                }
                else
                {
                    builder.Register(x => factory()).Named(contract, serviceType).AsImplementedInterfaces();
                }
            });
        }

        public IDisposable ServiceRegistrationCallback(Type serviceType, string contract, Action<IDisposable> callback)
        {
            // this method is not used by RxUI
            throw new NotImplementedException();
        }
    }
}