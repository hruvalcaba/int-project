using Autofac;
using System.Web.Mvc;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Plugin.Sync.Cklass.ActionFilters;

namespace Nop.Plugin.Sync.Cklass.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<ShoppingCartActionFilter>().As<IFilterProvider>();
            builder.RegisterType<OrderCklassWarehouseActionFilter>().As<IFilterProvider>();
            builder.RegisterType<ProductDetailsActionFilter>().As<IFilterProvider>();
            builder.RegisterType<CustomerActionFilter>().As<IFilterProvider>();

            //builder.RegisterType<TestActionFilter>().As<IFilterProvider>();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}
