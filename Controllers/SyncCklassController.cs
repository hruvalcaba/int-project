using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Sync.Cklass.Controllers
{
    [AdminAuthorize]
    public class SyncCklassController : BasePluginController
    {
        public ActionResult Configure()
        {
            return View("/Plugins/Sync.Cklass/Views/CklassConfig/Configure.cshtml");
        }
    }
}
