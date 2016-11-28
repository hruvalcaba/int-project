using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace Nop.Plugin.Sync.Cklass.Core
{

    public static class Settings
    {
        public static string GET_WS_URL()
        {
            return "https://pedidos.cklass.net/AppDebug/ServicesMov.svc/";
        }
    }
}
