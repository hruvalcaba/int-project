using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Sync.Cklass.Models
{
    [DataContract]
    public class AddProductToCartResult
    {
        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string AddToCartWarnings { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string PopupTitle { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string ProductAddedToCartWindow { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string RedirectUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [DataMember]
        public string Status { get; set; }
    }
}
