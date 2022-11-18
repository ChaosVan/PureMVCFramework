using PureMVC.Patterns.Observer;

namespace PureMVCFramework
{
    public class Updatable : Notifier
    {
        private bool enabled;
        private ITimerTask updateTask;

        public void EnableUpdate(bool tf, string taskName = "")
        {
            if (tf != enabled)
            {
                enabled = tf;
                if (enabled)
                    updateTask = TimerManager.Instance.AddFrameExecuteTask(Update, taskName);
                else
                    updateTask.Stop();
            }
        }

        private bool Update()
        {
            OnUpdate(TimerManager.Instance.DeltaTime);

            return !enabled;
        }
        protected virtual void OnUpdate(float deltaTime) { }
    }
}
