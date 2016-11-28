using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web.Mvc;
using Nop.Services.Catalog;
using Nop.Core.Infrastructure;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Catalog;
using Nop.Core;
using Nop.Services.Logging;
using System.Web.Routing;
using Nop.Plugin.Sync.Cklass.Models;
using Nop.Core.Plugins;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Sync.Cklass.Core
{
    public static class Utils
    {
        public static JsonResult SetCallBackWarningMessage(string message, bool isAjax = false)
        {
            var _logger = EngineContext.Current.Resolve<ILogger>();
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _pluginFinder = EngineContext.Current.Resolve<IPluginFinder>();

            var isAjaxCart = _pluginFinder.GetPluginDescriptorBySystemName("SevenSpikes.Nop.Plugins.QuickView");

            if (isAjax)
            {
                return new JsonResult
                {
                    Data = new AddProductToCartResult
                    {
                        AddToCartWarnings = "<ul><li>" + message + "</li></ul>",
                        ErrorMessage = "",
                        PopupTitle = "Add to cart failed due to following warnings",
                        ProductAddedToCartWindow = null,
                        RedirectUrl = "",
                        Status = "warning"
                    },

                    JsonRequestBehavior = JsonRequestBehavior.DenyGet
                };
            }

            return new JsonResult
            {
                Data = new
                {
                    success = false,
                    error = 1,
                    message = new List<string> { message }
                },

                JsonRequestBehavior = JsonRequestBehavior.DenyGet
            };
        }
        
        public static Product GetProductInRouteData(RouteData routData)
        {
            var _productService = EngineContext.Current.Resolve<IProductService>();
            int productID = Convert.ToInt32(routData.Values["productId"]);
            return _productService.GetProductById(productID);
        }

        public static Hashtable ParseAttributesToHashtable(String AttributesXML)
        {
            Hashtable OutAttributes = new Hashtable();
            IProductAttributeParser _productAttributeParser = EngineContext.Current.Resolve<IProductAttributeParser>();
            var attributes = _productAttributeParser.ParseProductAttributeValues(AttributesXML);
            foreach (var att in attributes)
            {
                OutAttributes.Add(att.ProductAttributeMapping.ProductAttribute.Name, att.Name);
            }
            return OutAttributes;
        }

        public static string GetProductoId(ShoppingCartItem sci)
        {
            return GetProductoId(sci.Product, sci.AttributesXml);
        }

        public static string GetProductoId(Product product, string AttributesXML)
        {
            Hashtable AttributeValues = ParseAttributesToHashtable(AttributesXML);
            string Sku = product.Sku ?? "";

            if (String.IsNullOrWhiteSpace(Sku))
                return null;

            try
            {
                string size = ParseToValidSize(AttributeValues["Size"].ToString());
                return Sku.Length == 6 ? Sku + "000" + (AttributeValues["Color"] ?? "000") + (size ?? "000") : Sku;
            }
            catch (NullReferenceException)
            {
                return product.Sku + "000000000";
            }
        }

        public static ProductAttributeCombination GetProductCombinationByProductId(string productoId)
        {
            var _prodictService = EngineContext.Current.Resolve<IProductService>();

            Product product = _prodictService.GetProductBySku(productoId.Substring(0,6));
            var combinations = product.ProductAttributeCombinations;

            return combinations.FirstOrDefault(x => x.Sku == productoId);
        }

        public static ShoppingCartItem GetSCIbyProductoId(string productoId, IList<ShoppingCartItem> cart)
        {
            var _prodictService = EngineContext.Current.Resolve<IProductService>();

            foreach (var sci in cart)
            {
                var pac = sci.Product.ProductAttributeCombinations.FirstOrDefault(x => x.Sku == productoId);
                if (sci.AttributesXml == pac.AttributesXml)
                    return sci;
            }

            return new ShoppingCartItem();
        }

        public static string ParseToValidSize(string SizeIn)
        {
            string patron = @"[\.\-\,]";
            Regex regex = new Regex(patron);
            string SizeOut = regex.Replace(SizeIn, "");
            if (SizeOut.Length < 3)
            {
                SizeOut = SizeOut.PadRight(3, '0');
            }

            return SizeOut.Substring(0,3);
        }
    }
}