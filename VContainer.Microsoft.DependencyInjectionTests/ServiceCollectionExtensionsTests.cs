using Microsoft.VisualStudio.TestTools.UnitTesting;
using VContainer.Microsoft.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Specification;
using Microsoft.Extensions.DependencyInjection;

namespace VContainer.Microsoft.DependencyInjection.Tests
{
    [TestClass()]
    public class ServiceCollectionExtensionsTests : DependencyInjectionSpecificationTests
    {
        protected override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            return serviceCollection.CreateVContainerServiceProvider();
        }
    }
}