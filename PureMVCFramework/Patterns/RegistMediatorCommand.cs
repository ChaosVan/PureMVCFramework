using System.Collections;
using System.Collections.Generic;
using PureMVC.Interfaces;
using PureMVC.Patterns.Command;
using PureMVCFramework.Advantages;
using UnityEngine;

namespace PureMVCFramework.Patterns
{
    public class RegistMediatorCommand : SimpleCommand
    {
        public const string Name = "RegistMediatorCommand";

        public override void Execute(INotification notification)
        {
            base.Execute(notification);

            object viewComponent = notification.Body;
            string mediatorName = notification.Type;

            if (string.IsNullOrEmpty(mediatorName))
                return;

            if (Facade.RetrieveMediator(mediatorName) != null)
            {
                Debug.LogErrorFormat("Type of {0} was exist", mediatorName);
                return;
            }

            IMediator mediator = ReferencePool.Instance.SpawnInstance(mediatorName, viewComponent) as IMediator;
            if (mediator != null)
            {
                mediator.ViewComponent = viewComponent;
                Facade.RegisterMediator(mediator);
            }
        }
    }
}
