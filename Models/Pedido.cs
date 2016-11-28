using Nop.Plugin.Sync.Cklass.Core;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Nop.Plugin.Sync.Cklass.Models
{
    public enum eEstatusId
    {
        Cancelar = 2,
        Cerrar = 1
    };

    /// <summary>
    /// 
    /// </summary>
    [DataContract]
    public class Pedido : ICommObject
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string PedidoId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string PedidoWebId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int ClienteNopId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int Cantidad { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public decimal Monto { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Estatus { get; set; }

        /// <summary>
        /// [0] : Cancelar,
        /// [1] : Cerrar
        /// </summary>
        [DataMember]
        public int EstatusId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string FechaEstatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public bool Success { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string MessageResult { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public List<DetPedido> DetalleCollection { get; set; }



        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string FechaVigencia { get; set; }
    }
}
