using Nop.Plugin.Sync.Cklass.Core;
using Nop.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Sync.Cklass.ActionFilters
{
    class ProductDetailsActionFilter : ActionFilterAttribute, IFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext,
            ActionDescriptor actionDescriptor)
        {
            if (controllerContext.Controller is ProductController && (
                actionDescriptor.ActionName.Equals("ProductDetails",
                StringComparison.InvariantCultureIgnoreCase)
                ))
            {
                return new List<Filter>() { new Filter(this, FilterScope.Action, 0) };
            }
            return new List<Filter>();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var product = Utils.GetProductInRouteData(filterContext.RouteData);

            if (product != null)
                SyncCklProducts.SycnStockProduct(product);
        } 
    }
}
