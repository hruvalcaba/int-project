using Nop.Plugin.Sync.Cklass.Core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nop.Plugin.Sync.Cklass.Models
{
    /// <summary>
    ///*********************************************************************************
    /// Proyecto: AppCklass
    /// Creacion por: JRAMIREZ
    /// Descripcion: Clase Producto que representa la estructura de un producto a devolver en el Servicio.
    ///*********************************************************************************
    /// </summary>
    [DataContract]
    public class Producto : ICommObject
    {
        #region Propiedades

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ProductoId { get; set; }


        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public decimal PrecioUnitario { get; set; }


        /// <summary>
        /// Descripcion: Combo campo agrupador de producto
        /// </summary>
        [DataMember]
        public string Combo { get; set; }

        /// <summary>
        /// Descripcion: Modelo del producto
        /// </summary>
        [DataMember]
        public string Modelo { get; set; }

        /// <summary>
        /// Descripcion: Indica si el producto sólo aplica para kits
        /// </summary>
        [DataMember]
        public bool SoloKit { get; set; }

        /// <summary>
        /// Descripcion: Material del producto
        /// </summary>
        [DataMember]
        public string MaterialId { get; set; }

        /// <summary>
        /// Descripcion: Descripcion del producto
        /// </summary>
        [DataMember]
        public string Descripcion { get; set; }

        /// <summary>
        /// Descripcion: Catalogo al que pertenece 
        /// </summary>
        [DataMember]
        public string Catalogo { get; set; }

        ///// <summary>
        ///// Descripcion: Tipo a que genero va dirigido el producto
        ///// </summary>
        //[DataMember]
        //public string Tipo { get; set; }

        ///// <summary>
        ///// Descripcion: Temporada del producto
        ///// </summary>
        //[DataMember]
        //public string Temporada { get; set; }

        /// <summary>
        /// Descripcion: Lista de los Colres por producto
        /// </summary>
        [DataMember]
        public List<Colores> Colores { get; set; }

        /// <summary>
        /// Descripcion: Lista de Tallas por producto
        /// </summary>
        [DataMember]
        public List<Tallas> Tallas { get; set; }

        ///// <summary>
        ///// Descripcion: Cantidades de existencia por talla
        ///// </summary>
        //[DataMember]
        //public List<Cantidades> Cantidades { get; set; }
        #endregion
    }

    /// <summary>
    ///*********************************************************************************
    /// Proyecto: AppCklass
    /// Creacion por: JRAMIREZ
    /// Descripcion: Clase Info que representa la estructura de la informacion que se obtiene de la sucursal
    ///*********************************************************************************
    /// </summary>
    [DataContract]
    public class Info
    {
        #region Propiedades
        /// <summary> 
        /// Descripcion: Agrupador de productos de paquete
        /// </summary>
        [DataMember]
        public string Combo { get; set; }

        /// <summary>
        /// Descripcion: Modelo del producto
        /// </summary>
        [DataMember]
        public string Modelo { get; set; }

        /// <summary>
        /// Descripcion: MaterialId del producto
        /// </summary>
        [DataMember]
        public string MaterialId { get; set; }

        /// <summary>
        /// Descripcion: ColorId del producto
        /// </summary>
        [DataMember]
        public string ColorId { get; set; }

        /// <summary>
        /// Descripcion: Talla del producto
        /// </summary>
        [DataMember]
        public string Talla { get; set; }

        /// <summary>
        /// Descripcion: ProductoId 
        /// </summary>
        [DataMember]
        public string ProductoId { get; set; }

        /// <summary>
        /// Descripcion: Color del producto.
        /// </summary>
        [DataMember]
        public string Color { get; set; }

        /// <summary>
        /// Descripcion: Descripcion del producto
        /// </summary>
        [DataMember]
        public string Descripcion { get; set; }

        /// <summary>
        /// Descripcion: Catalogo del producto
        /// </summary>
        [DataMember]
        public string Catalogo { get; set; }

        /// <summary>
        /// Descripcion: Ropa/Zapato
        /// </summary>
        [DataMember]
        public string Tipo { get; set; }

        /// <summary>
        /// Descripcion: Codigo de temporada 12,10,0
        /// </summary>
        [DataMember]
        public string Temporada { get; set; }

        /// <summary>
        /// Descripcion: Cantidad en existencia
        /// </summary>
        [DataMember]
        public double Cantidad { get; set; }
        #endregion
    }

    /// <summary>
    ///*********************************************************************************
    /// Proyecto: AppCklass
    /// Creacion por: JRAMIREZ
    /// Descripcion: Clase Colores es la estructura para generar listado de Colores
    ///*********************************************************************************
    /// </summary>
    [DataContract]
    public class Colores
    {
        #region Propiedades
        /// <summary>
        /// Descripcion: ColorId del color
        /// </summary>
        [DataMember]
        public string ColorId { get; set; }

        /// <summary>
        /// Descripcion: Descripcion del color
        /// </summary>
        [DataMember]
        public string Color { get; set; }
        #endregion
    }

    /// <summary>
    ///*********************************************************************************
    /// Proyecto: AppCklass
    /// Creacion por: JRAMIREZ
    /// Descripcion: Clase Tallas que representa la estructura de un talla a devolver en el Servicio.
    ///*********************************************************************************
    /// </summary>
    [DataContract]
    public class Tallas
    {
        #region Propiedades
        /// <summary>
        /// Descripcion: Talla del producto
        /// </summary>
        [DataMember]
        public string Talla { get; set; }
        #endregion
    }

    /// <summary>
    ///*********************************************************************************
    /// Proyecto: AppCklass
    /// Creacion por: JRAMIREZ
    /// Descripcion: Clase Cantidades que representa la estructura de un producto y su cantidad a devolver en el Servicio.
    ///*********************************************************************************
    /// </summary>
    [DataContract]
    public class Cantidades
    {
        #region Propiedades
        /// <summary>
        /// Descripcion: ProductoId
        /// </summary>
        [DataMember]
        public string ProductoId { get; set; }

        /// <summary>
        /// Descripcion: Existencia del producto
        /// </summary>
        [DataMember]
        public double Cantidad { get; set; }
        #endregion
    }
}