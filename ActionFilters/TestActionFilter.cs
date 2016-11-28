using Nop.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Nop.Plugin.Sync.Cklass.ActionFilters
{
    class TestActionFilter : ActionFilterAttribute, IFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            if (controllerContext.Controller is ShoppingCartController && (
                // Casos especiales para el AjaxCart de SevenSpikes
                actionDescriptor.ActionName.Equals("AddProductFromProductDetailsPageToCartAjax",
                StringComparison.InvariantCultureIgnoreCase) ||
                actionDescriptor.ActionName.Equals("AddProductToCartAjax",
                StringComparison.InvariantCultureIgnoreCase) ||
                // fin Casos especiales para el AjaxCart de SevenSpikes
                actionDescriptor.ActionName.Equals("AddProductToCart_Details",
                StringComparison.InvariantCultureIgnoreCase) ||
                actionDescriptor.ActionName.Equals("AddProductToCart_Catalog",
                StringComparison.InvariantCultureIgnoreCase) ||
                actionDescriptor.ActionName.Equals("Cart",
                StringComparison.InvariantCultureIgnoreCase) ||
                actionDescriptor.ActionName.Equals("Wishlist",
                StringComparison.InvariantCultureIgnoreCase)
                ))
            {
                return new List<Filter>() { new Filter(this, FilterScope.Action, 0) };
            }
            return new List<Filter>();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var filter = filterContext;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
        }
    }
}
