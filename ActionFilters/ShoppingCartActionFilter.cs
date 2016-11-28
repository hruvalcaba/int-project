using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Sync.Cklass.Core;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Nop.Plugin.Sync.Cklass.ActionFilters
{
    public class ShoppingCartActionFilter : ActionFilterAttribute, IFilterProvider
    {
        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext,
            ActionDescriptor actionDescriptor)
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
            #region Contexts & Services 

            var _logger = EngineContext.Current.Resolve<ILogger>();
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var _productService = EngineContext.Current.Resolve<IProductService>();
            var _shoppingCartService = EngineContext.Current.Resolve<IShoppingCartService>();
            var _atrrbitubeParser = EngineContext.Current.Resolve<IProductAttributeParser>();
            #endregion

            #region variables necesarias

            string WarningsCart = "";

            int ProductId = 0,
                ShoppingCartTypeId = 0, 
                FormQuantity = 0, 
                QuantityToCheck = 0;

            bool isFormDetails = false, 
                 isFormAjaxCart = false;

            FormCollection Form = new FormCollection();

            #endregion

            switch (filterContext.ActionDescriptor.ActionName)
            {
                #region AddToCart

                case "AddProductFromProductDetailsPageToCartAjax":
                case "AddProductToCartAjax":
                case "AddProductToCart_Details":
                case "AddProductToCart_Catalog":

                    #region Variables especiales
                    
                    string ProducoIdToDelete = "" , FormAttributesXML = "";

                    #endregion

                    #region Set vars

                    ProductId = Convert.ToInt32(filterContext.ActionParameters["productId"]);

                    if (filterContext.ActionParameters.ContainsKey("isAddToCartButton"))
                    {   // AJAX CART
                        isFormAjaxCart = true;
                        ShoppingCartTypeId = (bool)filterContext.ActionParameters["isAddToCartButton"] ?
                                            (int)ShoppingCartType.ShoppingCart : (int)ShoppingCartType.Wishlist;
                        filterContext.RequestContext.RouteData.Values.Add("shoppingCartTypeId", ShoppingCartTypeId);
                    }
                    else
                        // NATIVE CART
                        ShoppingCartTypeId = Convert.ToInt32(filterContext.ActionParameters["shoppingCartTypeId"]);

                    if (filterContext.ActionParameters.ContainsKey("form"))
                    {
                        // Details 
                        isFormDetails = true;
                        Form = (FormCollection)filterContext.ActionParameters["form"];
                    }
                    else
                        // Catalog
                        FormQuantity = Convert.ToInt32(filterContext.ActionParameters["quantity"]);

                    #endregion

                    if (ProductId > 0 && ShoppingCartTypeId == (int)ShoppingCartType.ShoppingCart)
                    {
                        Product ProductToAdd = _productService.GetProductById(ProductId);

                        #region Details

                        if (isFormDetails)
                        {
                            #region Extraccion de informacion desde el formulario

                            FormQuantity = Convert.ToInt32(Form[string.Format("addtocart_{0}.EnteredQuantity", ProductToAdd.Id)]);
                            int UpdateCartItemId = Convert.ToInt32(Form[string.Format("addtocart_{0}.UpdatedShoppingCartItemId", ProductToAdd.Id)]);

                            if (FormQuantity == -1)
                            {
                                WarningsCart = "Lo sentimos, no se puede leer la cantidad solicitada correctamente";
                                break;
                            }

                            #endregion
#if DEBUG
                            #region DEBUG LOGGING

                            _logger.Information("DEBUG > CheckCklWH_AF > get info from Form > Quantity: " + FormQuantity + " updatecartitemid: " + UpdateCartItemId);

                            #endregion
#endif
                            #region AttributesXML 

                            //product and gift card attributes
                            FormAttributesXML = ParseProductAttributes(ProductToAdd, Form);

                            #endregion

                            #region Actual Cart

                            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                                    .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart)
                                    .LimitPerStore(_storeContext.CurrentStore.Id)
                                    .ToList();

                            #endregion

                            // Es actualizacion de atributos
                            if (UpdateCartItemId > 0)
                            {
                                var SCIToUpdate = cart.FirstOrDefault(x => x.Id == UpdateCartItemId);

                                var OtherSCIWithSameParameters = _shoppingCartService.FindShoppingCartItemInTheCart(
                                                                            shoppingCart: cart,
                                                                            shoppingCartType: ShoppingCartType.ShoppingCart,
                                                                            product: ProductToAdd,
                                                                            attributesXml: FormAttributesXML);
                                if (SCIToUpdate != null)
                                {
                                    if (OtherSCIWithSameParameters != null &&
                                        OtherSCIWithSameParameters.Id == SCIToUpdate.Id)
                                    {
                                        // Es el mismo producto por lo que solo se actualiza la cantidad
                                        // Si la cantidad ingresada en el formulario es mayor se verificara si existe
                                        // en el almacen la diferencia que se desea agregar, de lo contrario puede ser
                                        // la misma cantidad o puede ser menor y no es necesario revisar en almacen
                                        QuantityToCheck = SCIToUpdate.Quantity < FormQuantity ? FormQuantity - SCIToUpdate.Quantity : 0;
                                    }
                                    else
                                    {
                                        // 1. Producto que no esta en el carrito (quantity del formulario)
                                        // 2. El producto Existe en el carrito con los mismos atributos y se actualiza la candiad
                                        //      NOTA: un bug del nopCommerce hace que al editar un producto A
                                        //              con las caracteriticas de otro producto B que ya esta en el carrito
                                        //              el producto B es actualizado solo con la cantidad que haya tenido
                                        //              el producto A al ser modificado.

                                        // Ya que los atributos fueron modificados, se agregan un producto nuevo con 
                                        // los nuevos atriburos o si ya existe un producto con los mismos atributos, se 
                                        // adicionara la cantidad, mostrada en este formulario, al producto ya existente.
                                        // Por lo tanto se va a verificar la existencia mostrada en el formulario
                                        QuantityToCheck = FormQuantity;

                                        // ELIMINACION DEL ANTERIOR
                                        ProducoIdToDelete = Utils.GetProductoId(SCIToUpdate);
                                    }
                                }
                                else
                                {
                                    // NO SE PUEDE OBTENER EL PRODUCTO A ACTUALIZAR... OSEA QUE PEDO??
                                    WarningsCart = "Lo sentimos, no tiene este producto en su carrito";
                                    break;
                                }
                            }
                            else
                            {
                                // 1. NO es actualiazion, asi que la cantidad es la misma que se muestra en el formulario
                                // 2. Se esta agregando un producto que ya existe en el carrito (sin ser actualización) asi
                                //      que se adicionara la cantidad mostrada en este formulario
                                QuantityToCheck = FormQuantity;
                            }

                            // Se envia la cantidad agregada y los atributos para el caso en el que no se
                            // puede agregar el producto al pedido en el sistema Cklass se realice un ROLLBACK
                            // Para los casos en los que se cambiaron atributos se envia el ProductoId para (liberar) eliminar
                            // el porducto anterior

                            filterContext.RouteData.Values.Add("QuantityAdded", QuantityToCheck);
                            filterContext.RouteData.Values.Add("AttributesXML", FormAttributesXML);
                            filterContext.RouteData.Values.Add("ProductoIdToDelete", ProducoIdToDelete);
                        }

                        #endregion

                        WarningsCart = SyncCklProducts.CheckProductAvailability(ProductToAdd, QuantityToCheck, FormAttributesXML);
                    }
                    break;

                #endregion

                #region Cart

                case "Cart":
                    if (filterContext.HttpContext.Request.HttpMethod.Equals("POST"))
                    {
                        string SkusToRemove = "", itemsCartIdToUpdate = "";

                        // Cart
                        Form = (FormCollection)filterContext.ActionParameters["form"];

                        if(Form.Count > 0)
                        {
                            var CartToUpdate = _workContext.CurrentCustomer.ShoppingCartItems
                                .Where(x => x.ShoppingCartType == ShoppingCartType.ShoppingCart)
                                .LimitPerStore(_storeContext.CurrentStore.Id)
                                .ToList();

                            var AllIdsToRemove = Form["removefromcart"]?.
                                                    Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.
                                                    Select(x => int.Parse(x))?.
                                                    ToList();

                            foreach(var sci in CartToUpdate)
                            {
                                #region Remove Items From a cart

                                if (AllIdsToRemove != null && AllIdsToRemove.Contains(sci.Id))
                                    SkusToRemove += Utils.GetProductoId(sci) + ":" + sci.AttributesXml + ",";

                                #endregion

                                #region Check quantity in cklass sys

                                if (Form.GetValue(string.Format("itemquantity{0}", sci.Id)) != null )
                                {
                                    int newQuantity = Convert.ToInt32(Form[string.Format("itemquantity{0}", sci.Id)]);
                                    
                                    // No hay modificaciones
                                    if (sci.Quantity != newQuantity)
                                    {
                                        // El peoducto se eliminara del pedio
                                        if (newQuantity == 0)
                                        {
                                            SkusToRemove += Utils.GetProductoId(sci) + ":" + sci.AttributesXml + ",";
                                        }
                                        else
                                        {
                                            // solo en caso de aumento se verifica la cantidad disponible en almacen 
                                            if (sci.Quantity < newQuantity)
                                            {
                                                QuantityToCheck = newQuantity - sci.Quantity;

                                                WarningsCart = SyncCklProducts.CheckProductAvailability(sci.Product, QuantityToCheck, sci.AttributesXml);
                                            }

                                            // en ambos casos (aumento o disminucion) se envia el id del producto para el siguiente paso
                                            itemsCartIdToUpdate += sci.Id + ",";
                                        }
                                    }
                                }
                                #endregion
                            }
                        }

                        filterContext.RouteData.Values.Add("SkusToRemove", SkusToRemove);
                        filterContext.RouteData.Values.Add("ItemsCartIdToUpdate", itemsCartIdToUpdate);
                    }
                    break;

                #endregion

                #region Wishlist

                case "Wishlist":
                    if (filterContext.HttpContext.Request.HttpMethod.Equals("POST"))
                    {
                        string ListSkusToAdd = "";

                        // Wishlist
                        Form = (FormCollection)filterContext.ActionParameters["form"];

                        if (Form.Count > 0)
                        {
                            var wishlist = _workContext.CurrentCustomer.ShoppingCartItems
                                    .Where(x => x.ShoppingCartType == ShoppingCartType.Wishlist)
                                    .LimitPerStore(_storeContext.CurrentStore.Id)
                                    .ToList();

                            var AllIdsToAdd = Form["addtocart"]?.
                                                    Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)?.
                                                    Select(x => int.Parse(x))?.
                                                    ToList();

                            foreach (var wci in wishlist)
                            {
                                if (AllIdsToAdd.Contains(wci.Id))
                                {
                                    WarningsCart = SyncCklProducts.CheckProductAvailability(wci.Product, wci.Quantity, wci.AttributesXml);
                                    ListSkusToAdd += Utils.GetProductoId(wci) + ",";

                                    // TODO: ERRORES
                                }
                            }
                            filterContext.RouteData.Values.Add("SkusToAdd", ListSkusToAdd);
                        }
                    }
                    break;

                    #endregion
            }

            if (WarningsCart.Length > 0)
            {
                filterContext.Result = Utils.SetCallBackWarningMessage(WarningsCart, isFormAjaxCart);
            }
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            #region Contexts & Services

            var _logger = EngineContext.Current.Resolve<ILogger>();
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var _productServices = EngineContext.Current.Resolve<IProductAttributeService>();
            var _shoppingCartService = EngineContext.Current.Resolve<IShoppingCartService>();

            #endregion

            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                            .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                            .LimitPerStore(_storeContext.CurrentStore.Id).ToList();

            switch (filterContext.ActionDescriptor.ActionName)
            {
                #region all Add's
                case "AddProductFromProductDetailsPageToCartAjax":
                case "AddProductToCartAjax":
                case "AddProductToCart_Details":
                case "AddProductToCart_Catalog":
                    string source = JsonConvert.SerializeObject(((JsonResult)filterContext.Result).Data);
                    dynamic data = JObject.Parse(source);
                    bool success = data.success ?? data.Status == "success";
                    int shoppingCartTypeId = Convert.ToInt32(filterContext.RouteData.Values["shoppingCartTypeId"]);


                    if (shoppingCartTypeId == (int)ShoppingCartType.ShoppingCart && success)
                    {
                        ShoppingCartItem itemAdded = new ShoppingCartItem();
                        string productoIdToDelete = filterContext.RouteData.Values["ProductoIdToDelete"]?.ToString();
                        string AttributesXML = filterContext.RouteData.Values["AttributesXML"]?.ToString();

                        var ProductAdded = _productServices.GetProductAttributeById
                                            (
                                                Convert.ToInt32(filterContext.RouteData.Values["productId"])
                                            );

                        itemAdded = String.IsNullOrEmpty(AttributesXML) ?
                                        cart.FirstOrDefault(x => x.ProductId == ProductAdded.Id) :
                                        cart.FirstOrDefault(x => x.AttributesXml == AttributesXML);

                        bool AddToCklOrder = SyncCklProducts.AddToOrder(itemAdded);

                        if (AddToCklOrder)
                        {
                            if (!String.IsNullOrEmpty(productoIdToDelete))
                                SyncCklProducts.DeleteOrderItems(new List<string> { productoIdToDelete });
                        }
                        else
                        {
                            // ROLLBACK en caso de que no se haya podido agregar el registro al pedido 
                            // en el servidor
                            var QuantityAdded = Convert.ToInt32(filterContext.RouteData.Values["QuantityAdded"]);
                            _shoppingCartService.UpdateShoppingCartItem(
                                                                _workContext.CurrentCustomer,
                                                                itemAdded.Id,
                                                                itemAdded.AttributesXml,
                                                                itemAdded.CustomerEnteredPrice,
                                                                itemAdded.RentalStartDateUtc,
                                                                itemAdded.RentalEndDateUtc,
                                                                itemAdded.Quantity - QuantityAdded,
                                                                true);
                        }
                    }
                    break;
                #endregion

                #region Cart

                case "Cart":
                    if (filterContext.HttpContext.Request.HttpMethod.Equals("POST"))
                    {
                        #region ELIMINA PRODUCTOS DESDE CARRITO

                        string SkusToRemove = filterContext.RouteData.Values["SkusToRemove"]?.ToString();
                        Dictionary<string, string> pa = new Dictionary<string, string>();

                        if (!String.IsNullOrWhiteSpace(SkusToRemove))
                        {
                            var lSKUAttributes = SkusToRemove.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                            foreach (string parskuAtt in lSKUAttributes)
                            {
                                var valores = parskuAtt.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                                if (valores.Count == 2)
                                    pa.Add(valores[0], valores[1]);
                            }
                            if (pa.Count > 0)
                            {
                                if (!SyncCklProducts.DeleteOrderItems(pa?.Keys?.ToList()))
                                {
                                    _logger.Warning("ShoppingCartActionFilter > Cart > Delete product", null , _workContext.CurrentCustomer);
                                    //TODO: ROLLBACK
                                }
                            }
                        }

                        #endregion

                        #region Actualizar cantidad de productos desde carrito

                        string ItemsCartIdToUpdate = filterContext.RouteData.Values["ItemsCartIdToUpdate"]?.ToString();

                        if (!String.IsNullOrWhiteSpace(ItemsCartIdToUpdate))
                        {
                            List<ShoppingCartItem> ItemsToUpdate = new List<ShoppingCartItem>();
                            var listSCIId = ItemsCartIdToUpdate.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                            foreach (string pid in listSCIId)
                            {
                                var sci = cart.FirstOrDefault(x => x.Id == Convert.ToInt32(pid));
                                if (sci != null)
                                    ItemsToUpdate.Add(sci);
                            }

                            if (!SyncCklProducts.UpdateOrderItems(ItemsToUpdate))
                            {
                                _logger.Warning("ShoppingCartActionFilter > Cart > UpdateOrderItems no se pudo actualizar", null, _workContext.CurrentCustomer);

                                //TODO: ROLLBACK
                            }
                        }

                        #endregion
                    }
                    break;

                #endregion

                #region Wishlist

                case "Wishlist":
                    if (filterContext.HttpContext.Request.HttpMethod.Equals("POST"))
                    {
                        string ItemsToAdd = filterContext.RouteData.Values["SkusToAdd"]?.ToString();

                        if (!String.IsNullOrWhiteSpace(ItemsToAdd))
                        {
                            List<ShoppingCartItem> SCIToUpdate = new List<ShoppingCartItem>();
                            List<string> items = ItemsToAdd.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                            foreach (var sci in cart)
                                if (items.Contains(Utils.GetProductoId(sci)))
                                    SCIToUpdate.Add(sci);

                            if (SCIToUpdate.Count > 0)
                                if (!SyncCklProducts.UpdateOrderItems(SCIToUpdate))
                                {
                                    _logger.Warning("ShoppingCartActionFilter > Cart > UpdateOrderItems no se pudo pasar de Wishlist a cart", null, _workContext.CurrentCustomer);
                                    //TODO: ROLLBACK
                                }
                        }
                    }
                    break;

                #endregion
            }
        }

        // This method is the same in ShoppingCartController
        // we need this for valitadions in updates
        private string ParseProductAttributes(Product product, FormCollection form)
        {
            var _productAttributeService = EngineContext.Current.Resolve<IProductAttributeService>();
            var _productAttributeParser = EngineContext.Current.Resolve<IProductAttributeParser>();
            var _downloadService = EngineContext.Current.Resolve<IDownloadService>();

            string attributesXml = "";

            #region Product attributes

            var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            foreach (var attribute in productAttributes)
            {
                string controlId = string.Format("product_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!String.IsNullOrEmpty(ctrlAttributes))
                            {
                                int selectedAttributeId = int.Parse(ctrlAttributes);
                                if (selectedAttributeId > 0)
                                    attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                        attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!String.IsNullOrEmpty(ctrlAttributes))
                            {
                                foreach (var item in ctrlAttributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    int selectedAttributeId = int.Parse(item);
                                    if (selectedAttributeId > 0)
                                        attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                            attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = _productAttributeService.GetProductAttributeValues(attribute.Id);
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                    attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!String.IsNullOrEmpty(ctrlAttributes))
                            {
                                string enteredText = ctrlAttributes.Trim();
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                    attribute, enteredText);
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            var day = form[controlId + "_day"];
                            var month = form[controlId + "_month"];
                            var year = form[controlId + "_year"];
                            DateTime? selectedDate = null;
                            try
                            {
                                selectedDate = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));
                            }
                            catch { }
                            if (selectedDate.HasValue)
                            {
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                    attribute, selectedDate.Value.ToString("D"));
                            }
                        }
                        break;
                    case AttributeControlType.FileUpload:
                        {
                            Guid downloadGuid;
                            Guid.TryParse(form[controlId], out downloadGuid);
                            var download = _downloadService.GetDownloadByGuid(downloadGuid);
                            if (download != null)
                            {
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                        attribute, download.DownloadGuid.ToString());
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            //validate conditional attributes (if specified)
            foreach (var attribute in productAttributes)
            {
                var conditionMet = _productAttributeParser.IsConditionMet(attribute, attributesXml);
                if (conditionMet.HasValue && !conditionMet.Value)
                {
                    attributesXml = _productAttributeParser.RemoveProductAttribute(attributesXml, attribute);
                }
            }

            #endregion

            #region Gift cards

            if (product.IsGiftCard)
            {
                string recipientName = "";
                string recipientEmail = "";
                string senderName = "";
                string senderEmail = "";
                string giftCardMessage = "";
                foreach (string formKey in form.AllKeys)
                {
                    if (formKey.Equals(string.Format("giftcard_{0}.RecipientName", product.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        recipientName = form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftcard_{0}.RecipientEmail", product.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        recipientEmail = form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftcard_{0}.SenderName", product.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        senderName = form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftcard_{0}.SenderEmail", product.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        senderEmail = form[formKey];
                        continue;
                    }
                    if (formKey.Equals(string.Format("giftcard_{0}.Message", product.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        giftCardMessage = form[formKey];
                        continue;
                    }
                }

                attributesXml = _productAttributeParser.AddGiftCardAttribute(attributesXml,
                    recipientName, recipientEmail, senderName, senderEmail, giftCardMessage);
            }

            #endregion

            return attributesXml;
        }
    }
}