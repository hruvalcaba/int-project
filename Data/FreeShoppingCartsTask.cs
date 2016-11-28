using Nop.Core;
using Nop.Core.Data;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Sync.Cklass.Core;
using Nop.Plugin.Sync.Cklass.Models;
using Nop.Services.Customers;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Sync.Cklass.Data
{
    class FreeShoppingCartsTask : ITask
    {
        private readonly ILogger _logger;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IRepository<ShoppingCartItem> _sciRepository;
        private readonly IStoreContext _storeContext;
        private readonly ICustomerService _customerService;

        #region Ctor

        public FreeShoppingCartsTask(ILogger logger,
                                  IShoppingCartService shoppingCartService,
                                  IRepository<ShoppingCartItem> sciRepository,
                                  IStoreContext storeContext, 
                                  ICustomerService customerService)
        {
            this._logger = logger;
            this._shoppingCartService = shoppingCartService;
            this._sciRepository = sciRepository;
            this._storeContext = storeContext;
            this._customerService = customerService;
        }
        #endregion

        public void Execute()
        {
            DateTime validatorDatetime = DateTime.UtcNow.AddMinutes(-20);
            List<ShoppingCartItem> SCItemsInvalid = new List<ShoppingCartItem>();
            List<Pedido> PedidosResponse = new List<Pedido>();
            List<int> CustomersIds = new List<int>();

            var Customers = _customerService.GetAllCustomers(
                                    loadOnlyWithShoppingCart: true,
                                    sct: ShoppingCartType.ShoppingCart
                                ).ToList();
#if DEBUG
            _logger.Information("DEBUG > Task > Eliminacion de Carritos invalidos x tiempo > run");
#endif

            foreach(var customer in Customers)
            {
                var Cart = customer.ShoppingCartItems
                            .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                            .LimitPerStore(_storeContext.CurrentStore.Id).ToList();

                var isInvalid = (from c in Cart
                                where c.UpdatedOnUtc < validatorDatetime
                                select c).Any();
                if(isInvalid)
                    CustomersIds.Add(customer.Id);
            }
            if(CustomersIds.Count > 0)
            {
                PedidosResponse = SyncCklProducts.CancelOrdersByNopCustomers(CustomersIds);

                foreach (var pedido in PedidosResponse)
                {
                    if (pedido.Success ||
                        pedido.MessageResult.Equals("No hay pedidos abiertos para este cliente.",
                            StringComparison.CurrentCultureIgnoreCase))
                    {
                        var customer = _customerService.GetCustomerById(pedido.ClienteNopId);

                        _logger.Debug("Se eliminara el carrito del usuario: " + customer.Id + " : " + customer.GetFullName());

                        SCItemsInvalid.AddRange(customer?.ShoppingCartItems
                                                    .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart)
                                                    );
                    }
                }

                foreach (var sciInvalid in SCItemsInvalid)
                {
                    _shoppingCartService.DeleteShoppingCartItem(sciInvalid);
                }
            }
        }
    }
}
