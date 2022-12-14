using System.Collections.Generic;

namespace PureMVCFramework
{
    public static class DomainReloadListConfig
    {
        [DomainReloadList("PureMVC")]
        public readonly static Dictionary<string, object> Configs = new Dictionary<string, object>()
        {
            {"PureMVC.Patterns.Facade.Facade.instance", null},
            {"PureMVC.Core.Model.instance", null},
            {"PureMVC.Core.View.instance", null},
            {"PureMVC.Core.Controller.instance", null},
        };
    }
}
