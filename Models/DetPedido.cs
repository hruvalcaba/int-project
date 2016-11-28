using System.Collections.Generic;
using System.Runtime.Serialization;
using Nop.Plugin.Sync.Cklass.Core;

namespace Nop.Plugin.Sync.Cklass.Models
{
    [DataContract]
    public class DetPedido : ICommObject
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public int Cantidad { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ComboId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public List<Producto> ProductoCollection { get; set; }
    }
}
