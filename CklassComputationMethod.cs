using System.Web.Routing;
using Nop.Core.Plugins;
using Nop.Core.Domain.Tasks;
using Nop.Services.Tasks;
using Nop.Services.Common;
using Nop.Services.Logging;

namespace Nop.Plugin.Sync.Cklass
{
    public class CklassComputationMethod : BasePlugin, IMiscPlugin
    {
        #region Services

        private readonly ILogger _logger;
        private readonly IScheduleTaskService _ScheduleTaskService;

        #endregion

        #region Ctor

        public CklassComputationMethod(ILogger logger, IScheduleTaskService ScheduleTaskService)
        {
            this._logger = logger;
            this._ScheduleTaskService = ScheduleTaskService;
        }

        #endregion

        #region overwrite methods

        public override void Install()
        {
            _ScheduleTaskService.InsertTask(new ScheduleTask()
            {
                Enabled = true,
                Name = "Actualización del inventario segun almacen Cklass",
                Seconds = (60 * 60) * 2,                //60 seg x 60 min = 3600 seg in 1 hour
                StopOnError = false,
                Type = "Nop.Plugin.Sync.Cklass.Data.CheckOutStocksTask, Nop.Plugin.Sync.Cklass"
            });

            _ScheduleTaskService.InsertTask(new ScheduleTask()
            {
                Enabled = true,
                Name = "Servicio de eliminacion de carritos por tiempo",
                Seconds = 60,                   // cada minuto
                StopOnError = false,
                Type = "Nop.Plugin.Sync.Cklass.Data.FreeShoppingCartsTask, Nop.Plugin.Sync.Cklass"
            });

            base.Install();
        }

        public override void Uninstall()
        {
            ScheduleTask task = _ScheduleTaskService
                                    .GetTaskByType("Nop.Plugin.Sync.Cklass.Data.CheckOutStocksTask, Nop.Plugin.Sync.Cklass");

            if (task != null)
            {
                _ScheduleTaskService.DeleteTask(task);
            }

            task = _ScheduleTaskService
                    .GetTaskByType("Nop.Plugin.Sync.Cklass.Data.FreeShoppingCartsTask, Nop.Plugin.Sync.Cklass");

            if (task != null)
            {
                _ScheduleTaskService.DeleteTask(task);
            }

            base.Uninstall();
        }

        #endregion

        #region plugin settings

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(
            out string actionName, 
            out string controllerName, 
            out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "SyncCklass";
            routeValues = new RouteValueDictionary { 
                                        { "Namespaces", "Nop.Plugin.Sync.Cklass.Controllers" }, 
                                        { "area", null } 
                                };
        }
        
        #endregion
    }
}