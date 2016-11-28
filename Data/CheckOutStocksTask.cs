using System.Collections.Generic;
using Nop.Services.Tasks;
using Nop.Services.Logging;
using Nop.Services.Catalog;
using Nop.Plugin.Sync.Cklass.Core;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Sync.Cklass
{
    public class CheckOutStocksTask : ITask
    {
        private readonly ILogger _logger;
        private readonly IProductService _productService;

        #region Ctor

        public CheckOutStocksTask(ILogger logger, 
                                  IProductService productService)
        {
            this._logger = logger;
            this._productService = productService;
        }
        #endregion

        public void Execute()
        {
            var LowStockProdutcsCombinations = _productService.GetLowStockProductCombinations();
            var LowStockProducts = _productService.GetLowStockProducts();

            List<Product> ProductToCheck = new List<Product>();

            foreach (var LowStockProductCom in LowStockProdutcsCombinations)
                if (! ProductToCheck.Contains(LowStockProductCom.Product))
                    ProductToCheck.Add(LowStockProductCom.Product);

            SyncCklProducts.CheckProductsAvailability(ProductToCheck);
        }
    }
}
