using Nop.Core;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Collections.Generic;
using Nop.Core.Infrastructure;
using Nop.Core.Domain.Customers;
using Nop.Services.Customers;
using Nop.Web.Controllers;
using Nop.Web.Models.Customer;
using Nop.Web.Framework.Security.Captcha;

using Nop.Plugin.Sync.Cklass.Core;
using Nop.Plugin.Sync.Cklass.Models;
using Nop.Services.Orders;

namespace Nop.Plugin.Sync.Cklass.ActionFilters
{
    class CustomerActionFilter : ActionFilterAttribute, IFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext,
            ActionDescriptor actionDescriptor)
        {
            if (controllerContext.Controller is CustomerController && (
                actionDescriptor.ActionName.Equals("Login",
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
            var _storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var _customerService = EngineContext.Current.Resolve<ICustomerService>();
            var _shoppingCartservice = EngineContext.Current.Resolve<IShoppingCartService>();
            var _customerRegistrationService = EngineContext.Current.Resolve<ICustomerRegistrationService>();
            
            var _customerSettings = EngineContext.Current.Resolve<CustomerSettings>();


            switch (filterContext.ActionDescriptor.ActionName)
            {
                case "Login":   
                    if (filterContext.ActionParameters.Count > 1 &&
                        ValidateLogin(Convert.ToBoolean(filterContext.RouteData.Values["captchaValid"]))
                        )
                    {
                        LoginModel model = filterContext.ActionParameters.ContainsKey("model") ? (LoginModel) filterContext.ActionParameters["model"] : new LoginModel();
                        Pedido pedidoResponser = new Pedido();

                        if (_customerSettings.UsernamesEnabled && model.Username != null)
                            model.Username = model.Username.Trim();

                        var loginResult = _customerRegistrationService.ValidateCustomer(_customerSettings.UsernamesEnabled ? model.Username : model.Email, model.Password);

                        if (loginResult == CustomerLoginResults.Successful)
                        {
                            var RegisteredCustomer = _customerSettings.UsernamesEnabled ? 
                                                _customerService.GetCustomerByUsername(model.Username) : 
                                                _customerService.GetCustomerByEmail(model.Email);

                            var GuestCustomer = _workContext.CurrentCustomer;

                            if (GuestCustomer.HasShoppingCartItems)
                            {
                                pedidoResponser = SyncCklProducts.MigrateOrderToRegisterUser(GuestCustomer.Id, RegisteredCustomer.Id);

                                if (!(bool)pedidoResponser?.Success)
                                {
                                    var cart = GuestCustomer.ShoppingCartItems;

                                    // Si no se pudo realizar una migracion correcta en Cklass se elimina el carrito
                                    while (cart.Any())
                                    {
                                        _shoppingCartservice.DeleteShoppingCartItem(cart.First());
                                    }
                                }
                            }
                                
                        }
                    }
                    break;
            }
        }

        private bool ValidateLogin(bool captchaValid)
        {
            var _captchaSettings = EngineContext.Current.Resolve<CaptchaSettings>();

            if (_captchaSettings.Enabled && _captchaSettings.ShowOnLoginPage && !captchaValid)
                return false;

            return true;
        }
    }
}
