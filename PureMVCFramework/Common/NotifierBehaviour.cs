using PureMVC.Interfaces;
using PureMVC.Patterns.Facade;
using UnityEngine;

namespace PureMVCFramework
{
    public class NotifierBehaviour : MonoBehaviour, INotifier
    {
        public void SendNotification(string notificationName, object body = null, string type = null)
        {
            Facade.SendNotification(notificationName, body, type);
        }

#if THREAD_SAFE
        public void SendNotificationSafe(string notificationName, object body = null, string type = null)
        {
            Facade.SendNotificationSafe(notificationName, body, type);
        }
#endif

        /// <summary>Return the Singleton Facade instance</summary>
        protected IFacade Facade => PureMVC.Patterns.Facade.Facade.GetInstance(() => new Facade());
    }
}
