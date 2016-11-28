using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Services.Orders;
using Nop.Web.Controllers;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using Nop.Services.Customers;
using Nop.Web.Framework.Security.Captcha;
using Nop.Web.Models.Customer;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Sync.Cklass.Core;
using Nop.Services.Payments;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Services.Logging;
using Nop.Web.Models.Checkout;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Catalog;

namespace Nop.Plugin.Sync.Cklass.ActionFilters
{
    public class OrderCklassWarehouseActionFilter : ActionFilterAttribute, IFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext,
            ActionDescriptor actionDescriptor)
        {
            if (controllerContext.Controller is CheckoutController && (
                actionDescriptor.ActionName.Equals("OpcConfirmOrder",
                StringComparison.InvariantCultureIgnoreCase) ||
                actionDescriptor.ActionName.Equals("Confirm", 
                StringComparison.InvariantCultureIgnoreCase)
                ))
            {
                return new List<Filter>() { new Filter(this, FilterScope.Action, 0) };
            }
            return new List<Filter>();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _orderService = EngineContext.Current.Resolve<IOrderService>();

            if (filterContext.HttpContext.Request.HttpMethod.Equals("POST"))
                switch (filterContext.ActionDescriptor.ActionName)
                {
                    case "Confirm":
                        if (ValidateOpcConfirmOrder())
                        {
                            var pedido = SyncCklProducts.CloseOrderByNopCustomer(_workContext.CurrentCustomer);
                            if (pedido.Success)
                            {
                                filterContext.RouteData.Values.Add("PedidoCklId", pedido.PedidoId);
                            }
                            else
                            {
                                filterContext.RouteData.Values.Add("WarningNote", "No se pudo pudo cerrar el pedido almancen...");
                            }
                        }
                        break;
                }
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _orderService = EngineContext.Current.Resolve<IOrderService>();
            var _storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var _orderSettings = EngineContext.Current.Resolve<OrderSettings>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            int orderId = 0;

            if (filterContext.Result is RedirectToRouteResult)
            {
                var result = (RedirectToRouteResult)filterContext.Result;
                try
                {
                    orderId = Convert.ToInt32(result.RouteValues["OrderId"]);
                }
                catch (Exception ex)
                {
                    _logger.Error("No se puede obtener el orderId", ex, _workContext.CurrentCustomer);
                }
                
            }

            if (orderId != 0)
                switch (filterContext.ActionDescriptor.ActionName)
                {                
                    case "Confirm":
                    //case "Confirm":
                        var lastOrder = _orderService.SearchOrders(
                                        storeId: _storeContext.CurrentStore.Id,
                                        customerId: _workContext.CurrentCustomer.Id
                                        ).FirstOrDefault();

                        if (lastOrder != null)
                        {
                            var interval = DateTime.UtcNow - lastOrder.CreatedOnUtc;
                            if (interval.TotalSeconds < _orderSettings.MinimumOrderPlacementInterval)
                            {
                                /*
                                string PedodoCklId = filterContext.RouteData.Values.ContainsKey("PedidoCklId") ?
                                                        filterContext.RouteData.Values["PedidoCklId"].ToString() : "";

                                 */
                                string PedodoCklId = filterContext.RouteData.Values["PedidoCklId"].ToString() ?? "";

                                if (!String.IsNullOrWhiteSpace(PedodoCklId))
                                {
                                    lastOrder.OrderNotes.Add(new OrderNote
                                    {
                                        Note = string.Format("Pedido No.{0}", PedodoCklId),
                                        DisplayToCustomer = true,
                                        CreatedOnUtc = DateTime.UtcNow
                                    });

                                    _orderService.UpdateOrder(lastOrder);
                                }
                                else
                                {
                                    string WarningNote = filterContext.RouteData.Values["WarningNote"].ToString() ?? "";
                                    if (String.IsNullOrWhiteSpace(WarningNote))
                                    {
                                        lastOrder.OrderNotes.Add(new OrderNote
                                        {
                                            Note = WarningNote,
                                            DisplayToCustomer = true,
                                            CreatedOnUtc = DateTime.UtcNow
                                        });

                                        _orderService.UpdateOrder(lastOrder);
                                    }
                                }
                            }
                        }

                        break;
                }
        }

        private bool ValidateOpcConfirmOrder()
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var _orderSettings = EngineContext.Current.Resolve<OrderSettings>();
            var _httpContext = EngineContext.Current.Resolve<HttpContextBase>();

            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(_storeContext.CurrentStore.Id)
                    .ToList();

            if (!cart.Any())
                return false;

            if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                return false;

            //prevent 2 orders being placed within an X seconds time frame
            if (!IsMinimumOrderPlacementIntervalValid(_workContext.CurrentCustomer))
                return false;

            return true;
        }

        protected virtual bool IsMinimumOrderPlacementIntervalValid(Customer customer)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _orderService = EngineContext.Current.Resolve<IOrderService>();
            var _storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var _orderSettings = EngineContext.Current.Resolve<OrderSettings>();

            //prevent 2 orders being placed within an X seconds time frame
            if (_orderSettings.MinimumOrderPlacementInterval == 0)
                return true;

            var lastOrder = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                .FirstOrDefault();
            if (lastOrder == null)
                return true;

            var interval = DateTime.UtcNow - lastOrder.CreatedOnUtc;
            return interval.TotalSeconds > _orderSettings.MinimumOrderPlacementInterval;
        }

    }
}
