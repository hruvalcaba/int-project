using Nop.Plugin.Sync.Cklass.Core;
using Nop.Plugin.Sync.Cklass.Models;
using System;
using System.Threading.Tasks;

namespace Nop.Plugin.Sync.Cklass.WebServices.Clients
{
    class ClientAgregarPedido : WebApiRequestorBase<Pedido>
    {
        private const string SEGMENT_URL = "AgregarPedidoNop",
                                ID_FIELD = "";

        public ClientAgregarPedido(string baseUrl) : base(baseUrl) { }

        public override Pedido Get(string id = null)
        {
            WebApiRequest req = new WebApiRequest()
            {
                Method = HttpMethod.GET,
                SegmentUrl = SEGMENT_URL + "/Get" + (string.IsNullOrEmpty(id) ? "" : ("?" + ID_FIELD + "=" + id))
            };
            return this.Request(req);
        }

        public override async Task<Pedido> GetAsync(string id = null)
        {
            WebApiRequest req = new WebApiRequest()
            {
                Method = HttpMethod.GET,
                SegmentUrl = SEGMENT_URL + "/Get" + (string.IsNullOrEmpty(id) ? "" : ("?" + ID_FIELD + "=" + id))
            };
            return await this.RequestAsync(req);
        }

        public override Pedido Post(Pedido model)
        {
            WebApiRequest request = new WebApiRequest();
            request.SegmentUrl = SEGMENT_URL;
            request.Method = HttpMethod.POST;
            request.Data = model;
            return this.Request(request);
        }

        public override Task<Pedido> PostAsync(Pedido model)
        {
            WebApiRequest request = new WebApiRequest();
            request.SegmentUrl = SEGMENT_URL;
            request.Method = HttpMethod.POST;
            request.Data = model;
            return this.RequestAsync(request);
        }

        public override Pedido Put(Pedido model)
        {
            throw new NotImplementedException();
        }

        public override Task<Pedido> PutAsync(Pedido model)
        {
            throw new NotImplementedException();
        }

        public override Pedido Delete(string id)
        {
            throw new NotImplementedException();
        }

        public override Task<Pedido> DeleteAsync(string id)
        {
            throw new NotImplementedException();
        }
    }
}
