using System;
using System.Collections.Generic;
using Autofac;
using Splat;

namespace PodBoy.Bootstrap
{
    public class ContainerBoy : ContainerBuilder, IMutableDependencyResolver
    {
        object IDependencyResolver.GetService(Type serviceType, string contract)
        {
            throw new NotImplementedException();
        }

        IEnumerable<object> IDependencyResolver.GetServices(Type serviceType, string contract)
        {
            throw new NotImplementedException();
        }

        void IMutableDependencyResolver.Register(Func<object> factory, Type serviceType, string contract)
        {
            if (string.IsNullOrEmpty(contract))
            {
                this.Register(x => factory()).As(serviceType).AsImplementedInterfaces();
            }
            else
            {
                this.Register(x => factory()).Named(contract, serviceType).AsImplementedInterfaces();
            }
        }

        IDisposable IMutableDependencyResolver.ServiceRegistrationCallback(Type serviceType,
            string contract,
            Action<IDisposable> callback)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            throw new NotImplementedException();
        }
    }
}