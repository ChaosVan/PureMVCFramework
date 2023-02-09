using PureMVC.Interfaces;
using PureMVC.Patterns.Command;

namespace PureMVCFramework.Patterns
{
    public class RemoveMediatorCommand : SimpleCommand
    {
        public const string Name = "RemoveMediatorCommand";

        public override void Execute(INotification notification)
        {
            base.Execute(notification);

            object componentObj = notification.Body;
            string mediatorName = notification.Type;

            if (string.IsNullOrEmpty(mediatorName))
                return;

            IMediator mediator = Facade.RemoveMediator(mediatorName);
            if (mediator != null)
            {
                mediator.ViewComponent = null;
                ReferencePool.RecycleInstance(mediator);
            }
        }
    }
}
