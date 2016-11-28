using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.Logging;
using Nop.Services.Events;
using Nop.Services.Orders;
using Nop.Core.Events;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Sync.Cklass.Core;

namespace Nop.Plugin.Sync.Cklass.Data
{
    public class ShoppingCartItemConsumer : IConsumer<EntityInserted<ShoppingCartItem>>, 
                                            IConsumer<EntityUpdated<ShoppingCartItem>>,
                                            IConsumer<EntityDeleted<ShoppingCartItem>>
    {
        private readonly ILogger _logger;
        private readonly IShoppingCartService _shoppingCartService;

        public ShoppingCartItemConsumer(ILogger logger, IShoppingCartService shopingcartService)
        {
            this._logger = logger;
            this._shoppingCartService = shopingcartService;
        }

        public void HandleEvent(EntityInserted<ShoppingCartItem> eventMessage)
        {
        }

        public void HandleEvent(EntityUpdated<ShoppingCartItem> eventMessage)
        {
        }

        public void HandleEvent(EntityDeleted<ShoppingCartItem> eventMessage)
        {
        }
    }
}
