#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PureMVCFramework
{
    public interface ITimerTask
    {
        bool Update(float deltaTime);
        bool Execute();
        void Stop();
    }

    public enum TimerType
    {
        FIXED_DURATION,
        FIXED_REALTIME_DURATION,
        EVERY_FRAME,
    }

    public class TimerManager : SingletonBehaviour<TimerManager>
    {
#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<ITimerTask> m_TaskList = new List<ITimerTask>();
        private readonly List<ITimerTask> m_ToBeRemoved = new List<ITimerTask>();

        public float DeltaTime { get; private set; }

        protected override void OnDelete()
        {
            m_TaskList.Clear();
            base.OnDelete();
        }

        protected override void OnUpdate(float delta)
        {
            DeltaTime = delta;
            for (int i = 0; i < m_TaskList.Count; ++i)
            {
                ITimerTask task = m_TaskList[i];
                if (task.Update(delta) && task.Execute())
                {
                    task.Stop();
                    m_ToBeRemoved.Add(task);
                }
            }

            if (m_ToBeRemoved.Count > 0)
            {
                for (int i = 0; i < m_ToBeRemoved.Count; ++i)
                {
                    m_TaskList.Remove(m_ToBeRemoved[i]);
                }
                m_ToBeRemoved.Clear();
            }
        }

        /// <summary>
        /// Adds the one shot task.
        /// </summary>
        /// <param name="startDelay">Start delay.</param>
        /// <param name="executable">Executable.</param>
        public ITimerTask AddOneShotTask(float startDelay, Action executable, string taskName = null)
        {
            return AddTask(TimerType.FIXED_DURATION, Time.time + startDelay, 0, 1, () =>
            {
                executable?.Invoke();
                return true;
            }, taskName);
        }

        /// <summary>
        /// Adds the realtime one shot task.
        /// </summary>
        /// <param name="startDelay">Start delay.</param>
        /// <param name="executable">Executable.</param>
        public ITimerTask AddRealtimeOneShotTask(float startDelay, Action executable, string taskName = null)
        {
            return AddTask(TimerType.FIXED_REALTIME_DURATION, Time.realtimeSinceStartup + startDelay, 0, 1, () =>
            {
                executable?.Invoke();
                return true;
            }, taskName);
        }

        /// <summary>
        /// Adds the repeat task.
        /// </summary>
        /// <param name="startDelay">Start delay.</param>
        /// <param name="interval">Interval.</param>
        /// <param name="repeatTimes">Repeat times, -1 means always.</param>
        /// <param name="executable">Executable.</param>
        public ITimerTask AddRepeatTask(float startDelay, float interval, int repeatTimes, Func<bool> executable, string taskName = null)
        {
            return AddTask(TimerType.FIXED_DURATION, Time.time + startDelay, interval, repeatTimes, executable, taskName);
        }

        /// <summary>
        /// Adds the realtime repeat task.
        /// </summary>
        /// <param name="startDelay">Start delay.</param>
        /// <param name="interval">Interval.</param>
        /// <param name="repeatTimes">Repeat times, -1 means always.</param>
        /// <param name="executable">Executable.</param>
        public ITimerTask AddRealtimeRepeatTask(float startDelay, float interval, int repeatTimes, Func<bool> executable, string taskName = null)
        {
            return AddTask(TimerType.FIXED_REALTIME_DURATION, Time.realtimeSinceStartup + startDelay, interval, repeatTimes, executable, taskName);
        }

        /// <summary>
        /// Adds the frame execute task.
        /// </summary>
        /// <param name="executable">Executable.</param>
        public ITimerTask AddFrameExecuteTask(Func<bool> executable, string taskName = null)
        {
            return AddTask(TimerType.EVERY_FRAME, Time.time, 0, 0, executable, taskName);
        }

        /// <summary>
        /// Adds the task.
        /// </summary>
        /// <returns>The task.</returns>
        /// <param name="timerType">Timer type.</param>
        /// <param name="startTime">Start time.</param>
        /// <param name="interval">Interval.</param>
        /// <param name="repeatTimes">Repeat times.</param>
        /// <param name="executable">Executable.</param>
        private ITimerTask AddTask(TimerType timerType, float startTime, float interval, int repeatTimes, Func<bool> executable, string taskName)
        {
            var task = new TimerTask
            {
                timerType = timerType,
                startTime = startTime,
                interval = interval,
                repeatTimes = repeatTimes,
                executable = executable,

#if UNITY_EDITOR
                name = string.IsNullOrEmpty(taskName) ? DefaultTaskName(executable) : taskName,
#endif
            };
            m_TaskList.Add(task);

            return task;
        }

#if UNITY_EDITOR
        private static string DefaultTaskName(Func<bool> executable)
        {
            var methodInfo = executable.GetMethodInfo();

            return methodInfo.DeclaringType.Name + "." + methodInfo.Name;
        }
#endif
    }

    internal struct TimerTask : ITimerTask, IDisposable
    {
        public string name;

        public TimerType timerType;
        public float startTime;
        public float interval;
        public int repeatTimes;
        public Func<bool> executable;

        private int executeTimes;
        private float elapsedTime;
        private float nextExecuteTime;
        private bool started;
        private bool stopped;

        public bool IsStopped { get => stopped; }

#if ODIN_INSPECTOR
        [ShowInInspector]
#endif
        public float ElapsedTime => elapsedTime;

        public void Stop()
        {
            stopped = true;
        }

        public void Dispose()
        {
            executeTimes = 0;
            elapsedTime = 0;
            nextExecuteTime = 0;
            started = false;
            stopped = false;
        }

        public bool Update(float delta)
        {
            elapsedTime += delta;

            float time = Time.time;
            if (timerType == TimerType.FIXED_REALTIME_DURATION)
                time = Time.realtimeSinceStartup;

            if (!started)
            {
                if (time >= startTime)
                {
                    started = true;
                    nextExecuteTime = startTime + interval;
                    return true;
                }
            }
            else
            {
                if (timerType == TimerType.EVERY_FRAME)
                    return true;

                if (time >= nextExecuteTime)
                {
                    nextExecuteTime += interval;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Execute this instance.
        /// </summary>
        /// <returns>if <see langword="true"/>, remove this task</returns>
        public bool Execute()
        {
            executeTimes++;
            if (executable != null)
            {
                try
                {
                    return stopped || executable() || executeTimes == repeatTimes;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            return true;
        }
    }
}
