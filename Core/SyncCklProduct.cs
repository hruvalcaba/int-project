using System;
using System.Linq;
using System.Collections.Generic;
using PSClients;
using PSModels.Communication;
using Nop.Core;
using Nop.Services.Logging;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Sync.Cklass.Models;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Sync.Cklass.WebServices.Clients;

namespace Nop.Plugin.Sync.Cklass.Core
{
    public class SyncCklProducts
    {
        public static SearchStockResponse SycnStockProduct(Product product)
        {
            var _logger = EngineContext.Current.Resolve<ILogger>();
            var _productAttributeService = EngineContext.Current.Resolve<IProductAttributeService>();
            string ErrorMessage = "", LogErrorMessage = "";

            string model = product.Sku ?? "";
            model = model.Length > 6 ? model.Substring(0, 6) : model;

            SearchStockResponse response = new SearchStockResponse();

            _logger.Information("SycnStockProduct > DEBUG > SycnStockProduct > model: " + model);

            if (!String.IsNullOrWhiteSpace(model) && model.Length == 6)
            {
                #region API Request
                try
                {
                    var client = new SearchStockClient("http://dev/ps/api");
                    client.RequestTimeout = 15000;
                    client.RequestKeepAlive = true;
                    response = client.Post(new SearchStockRequest()
                    {
                        Model = model,
                        IncludeProductsWithoutStock = true,
                        Stores = new string[] { "DEV 010" }
                    });

                    _logger.Information("SycnStockProduct > DEBUG > SycnStockProduct > response: " + response.Success);

                }
                catch (Exception ex)
                {
                    _logger.Error("SycnStockProduct > API stock: " + ex.Message, ex);
                    ErrorMessage = "Error al conectar con el servidor de pedidos";
                }
                #endregion

                if (response.Success)
                {
                    if (response.Stock.Count != 0)
                    {
                        #region Sincronizacion de stock NopCommerce con Almacen Cklass

                        // Se suman todas las cantidades de disponibilidad sin importar
                        // Caracteristicas y se actualiza el stock del modelo en NopCommerce
                        product.StockQuantity = Convert.ToInt32(response.Stock.Sum(item => item.CantDisp));

                        // si el producto no tiene existencias se desactiva el boton de compra ("agregar al carrito")
                        // product.DisableBuyButton = product.StockQuantity == 0;

                        // se actualizan cantidades de productos especificos si existen
                        foreach (ProductAttributeCombination pac in product.ProductAttributeCombinations)
                        {
                            // ESTO NO ES NECESARIO SI AL ALTA DEL PRODUCTO SE AGREGA CORRECTAMENTE EL SKU SEGUN ATRIBUTOS
                            if (pac.Sku == null || pac.Sku.Length != 15)
                            {
                                pac.Sku = Utils.GetProductoId(product, pac.AttributesXml);
                            }

                            var IN_INVLOC = response.Stock.FirstOrDefault(x => x.Producto.ProductoID == pac.Sku);
                            if (IN_INVLOC != null)
                            {
                                pac.StockQuantity = Convert.ToInt32(IN_INVLOC.CantDisp);
                                _productAttributeService.UpdateProductAttributeCombination(pac);
                            }
                            else
                            {
                                pac.StockQuantity = 0;
                                LogErrorMessage = "La combinacion: " + pac.Sku + "del producto con id: " + product.Id +" no tiene referencia en almacen";
                                //ErrorMessage = "El modelo espeficado no se encuentra en el almacen";
                            }
                        }
                        #endregion
                    }
                    else
                    {
                        LogErrorMessage = "ProductoId: " + product.Id + " con Sku: " + product.Sku + " no tiene referencia en almacen";
                        //ErrorMessage = "El modelo no se encuentra en el almacen";
                    }
                }
                else
                {
                    LogErrorMessage = response.Error != null ? response.Error.Message : "";
                    ErrorMessage = "Imposible conectar con el almacen";
                }
            }
            else
            {
                // En la base dade datos local no se ingreso correctamente el SKU (deben ser solo el MODELO)
                LogErrorMessage = "El producto con id: " + product.Id + " tiene un SKU invalido ( " + (product.Sku ?? "No capturado") + " )";
                ErrorMessage = "No se puede agregar este producto al almacen...";
            }
            
            if (!String.IsNullOrWhiteSpace(ErrorMessage))
            {
                response.Success = false;
                response.Error = new ApiError(ErrorMessage, ErrorCode.CLIENT_ERROR);
                _logger.Warning("ProductStockSync > " + LogErrorMessage);
            }
            return response;
        }

        public static string CheckProductAvailability(Product product, int quantity, string AttributesXML = null)
        {
            var response = SycnStockProduct(product);

            if (response.Success)
            {
                #region filtering features (armado del producto ID Especifico)

                string productId = Utils.GetProductoId(product, AttributesXML);

                #endregion

                var _productWH = response.Stock.FirstOrDefault(x => x.Producto.ProductoID.StartsWith(productId));

                if (_productWH != null)
                {
                    if (_productWH.CantDisp >= quantity)
                        return "";
                    // No hay existencia suficiente para completar el pedido solicitado 
                    return "";
                }
                // no se encuentra la combinacion enviada en el almacen No se encuentra la combinación Talla/Color en almacen
                return "";
            }
            return response.Error.Message;
        }

        public static bool AddToOrder(ShoppingCartItem itemToAdd)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            string productID = Utils.GetProductoId(itemToAdd);

            _logger.Information("itemToAdd > DEBUG > itemToAdd > productID: " + productID);

            #region Consumo WS bloqueo de productos (se agrega a un pedido)

            ClientAgregarPedido serv = new ClientAgregarPedido(Settings.GET_WS_URL());

            Producto[] producto = { new Producto { ProductoId = productID } };
            DetPedido[] detalleP = { new DetPedido { Cantidad = itemToAdd.Quantity, ProductoCollection = producto.ToList() } };

            Pedido pedido = new Pedido
            {
                ClienteNopId = _workContext.CurrentCustomer.Id,
                DetalleCollection = detalleP.ToList(),
            };
            serv.RequestKeepAlive = true;
            Pedido responser = new Pedido();

            try
            {
                responser = serv.Post(pedido);
            }
            catch(Exception ex)
            {
                _logger.Error("No se puede conectar con el servidor para bloquear el producto", ex, _workContext.CurrentCustomer);
            }

#if DEBUG
            if (responser.Success)
                _logger.Information("itemToAdd > DEBUG > Se bloqueo el producto: " + itemToAdd.Product.Name + " Cantidad: " + itemToAdd.Quantity);
            else
                _logger.Information("itemToAdd > DEBUG > no se pudo agregar bloquear: " + responser.MessageResult);
#endif

            return responser.Success;

            #endregion
        }

        public static bool DeleteOrderItems(List<string> ProductosIds)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            ClientAgregarPedido serv = new ClientAgregarPedido(Settings.GET_WS_URL());

            List<DetPedido> detallesPedido = new List<DetPedido>();

            foreach (string pid in ProductosIds)
                if (pid.Length > 0)
                    detallesPedido.Add(new DetPedido
                    {
                        Cantidad = 0,
                        ProductoCollection = new List<Producto> { new Producto { ProductoId = pid } }
                    });

            Pedido pedido = new Pedido
            {
                ClienteNopId = _workContext.CurrentCustomer.Id,
                DetalleCollection = detallesPedido.ToList(),
            };
            serv.RequestKeepAlive = true;

            Pedido responser = new Pedido();
            try
            {
                responser = serv.Post(pedido);
            }
            catch (Exception ex)
            {
                _logger.Error("SyncCklProduct > DeleteOrderItems > " + ex.Message, ex, _workContext.CurrentCustomer);
            }

#if DEBUG
            _logger.Information("Se elimino el bloqueo el producto(s): " + string.Join(",", ProductosIds.ToArray()));
#endif

            return responser.Success;
        }

        public static bool UpdateOrderItems(List<ShoppingCartItem> productos)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            ClientAgregarPedido serv = new ClientAgregarPedido(Settings.GET_WS_URL());

            List<DetPedido> detallesPedido = new List<DetPedido>();

            foreach (ShoppingCartItem item in productos)
            {
                string productID = Utils.GetProductoId(item);

                detallesPedido.Add(new DetPedido
                {
                    Cantidad = item.Quantity,
                    ProductoCollection = new List<Producto> { new Producto { ProductoId = productID } }
                });
            }

            Pedido pedido = new Pedido
            {
                ClienteNopId = _workContext.CurrentCustomer.Id,
                DetalleCollection = detallesPedido.ToList(),
            };
            serv.RequestKeepAlive = true;

            Pedido responser = new Pedido();

            try
            {
                responser = serv.Post(pedido);
            }
            catch(Exception ex)
            {
                _logger.Error("SyncCklProduct > UpdateOrderItems > " + ex.Message, ex, _workContext.CurrentCustomer);
            }

            return responser.Success;
        }
        
        public static void CheckProductsAvailability(List<Product> Products)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();
            var _productService = EngineContext.Current.Resolve<IProductService>();

            var models = (from p in Products
                          select p.Sku)
                            .ToList();

            List < ResponseProductoDisp > responser = new List<ResponseProductoDisp>();

            try
            {
                responser = HttpUtils.Request<List<ResponseProductoDisp>>(Settings.GET_WS_URL() + "VerificarProductoDisp", HttpMethod.POST, models);
            }
            catch(Exception ex)
            {
                _logger.Error("SyncCklProduct > CheckProductsAvailability > " + ex.Message, ex, _workContext.CurrentCustomer);
            }

            if (responser?.Count > 0)
            {
                foreach(var product in Products)
                {
                    foreach (var pac in product.ProductAttributeCombinations)
                    {
                        var quantity = responser.FirstOrDefault(x => x.ProductoId == pac.Sku)?.CantDisp;
                        if(quantity != null)
                            pac.StockQuantity = quantity ?? 0;
                        else
                        {
                            _logger.Warning("SyncCklProduct > CheckProductsAvailability > " + pac.Sku + " no tiene referencia en almacen");
                        }
                    }
                }
            }
        }

        public static Pedido MigrateOrderToRegisterUser(int formCustomerId, int toCustomerId)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            ClientUpdateUsrOrder cte = new ClientUpdateUsrOrder(Settings.GET_WS_URL());
            Pedido responser = new Pedido();

            try
            {
                responser = cte.migrar(formCustomerId, toCustomerId);
            }
            catch (Exception ex)
            {
                _logger.Error("SyncCklProduct > MigrateOrderToRegisterUser > " + ex.Message, ex, _workContext.CurrentCustomer);
            }

#if DEBUG
            _logger.Information("Se cambio el pedido del usuario con id: " + formCustomerId + " al usuario con id: " + toCustomerId);
#endif
            return responser;
        }

        public static Pedido CloseOrderByNopCustomer(Customer customer)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            ClientCambiarEstatusPedido serv = new ClientCambiarEstatusPedido(Settings.GET_WS_URL());

            Pedido responser = new Pedido();

            try
            {
                responser = serv.CambiarEstatus(customer.Id, (int) eEstatusId.Cerrar);
            }
            catch (Exception ex)
            {
                _logger.Error("SyncCklProduct > CloseOrderByNopCustomer > " + ex.Message, ex, _workContext.CurrentCustomer);
            }

#if DEBUG
            _logger.Information("TASK: Se cerrara (inica proceso de venta formal) el pedido del usuario con id: " + customer.Id);
#endif

            return responser;
        }
        
        public static Pedido CancelOrderByNopCustomer(Customer customer)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            ClientCambiarEstatusPedido serv = new ClientCambiarEstatusPedido(Settings.GET_WS_URL());

            Pedido responser = new Pedido();

            try
            {
                responser = serv.CambiarEstatus(customer.Id, (int) eEstatusId.Cancelar);
            }
            catch (Exception ex)
            {
                _logger.Error("SyncCklProduct > CancelOrderByNopCustomer > " + ex.Message, ex, _workContext.CurrentCustomer);
            }

#if DEBUG
            _logger.Information("Se cancelara el pedido del usuario con id: " + customer.Id);
#endif

            return responser;
        }

        public static List<Pedido> CancelOrdersByNopCustomers(List<int> CustomersIds)
        {
            var _workContext = EngineContext.Current.Resolve<IWorkContext>();
            var _logger = EngineContext.Current.Resolve<ILogger>();

            List<Pedido> resultados = new List<Pedido>();

            var RequestData = new RequestCambiarEstatus
            {
                ClienteNopId = CustomersIds,
                EstatusId = (int) eEstatusId.Cancelar
            };

            try
            {
                resultados = HttpUtils.Request<List<Pedido>>(Settings.GET_WS_URL() + "CambiarEstatusPedido", HttpMethod.POST, RequestData);
            }
            catch (Exception ex)
            {
                _logger.Error("SyncCklProduct > CancelOrdersByNopCustomers > " + ex.Message, ex, _workContext.CurrentCustomer);
            }
            return resultados;
        }

        // TODO
        public static bool GetActiveOrderByNopCustomer(Customer customer)
        {
            return true;
        }
    }
}
