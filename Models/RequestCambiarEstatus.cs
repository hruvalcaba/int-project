using System.Collections.Generic;

namespace Nop.Plugin.Sync.Cklass.Models
{
    public class RequestCambiarEstatus
    {
        public List<int> ClienteNopId { get; set; }
        public int EstatusId { get; set; }
    }
}
