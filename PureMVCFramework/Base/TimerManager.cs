#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PureMVCFramework
{
    public class TimerManager : SingletonBehaviour<TimerManager>
    {
        public enum TimerType
        {
            FIXED_DURATION,
            FIXED_REALTIME_DURATION,
            EVERY_FRAME,
        }

        public class TimerTask : IDisposable
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

            internal bool Update(float delta)
            {
                //if (timerType == TimerType.FIXED_DURATION)
                //    elapsedTime += delta;

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
            internal bool Execute()
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

#if ODIN_INSPECTOR
        [ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
        private readonly List<TimerTask> m_TaskList = new List<TimerTask>();
        private readonly List<TimerTask> m_ToBeRemoved = new List<TimerTask>();

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
                TimerTask task = m_TaskList[i];
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
                    ReferencePool.RecycleInstance(m_ToBeRemoved[i]);
                }
                m_ToBeRemoved.Clear();
            }
        }

        /// <summary>
        /// Adds the one shot task.
        /// </summary>
        /// <param name="startDelay">Start delay.</param>
        /// <param name="executable">Executable.</param>
        public TimerTask AddOneShotTask(float startDelay, Action executable, string taskName = null)
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
        public TimerTask AddRealtimeOneShotTask(float startDelay, Action executable, string taskName = null)
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
        public TimerTask AddRepeatTask(float startDelay, float interval, int repeatTimes, Func<bool> executable, string taskName = null)
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
        public TimerTask AddRealtimeRepeatTask(float startDelay, float interval, int repeatTimes, Func<bool> executable, string taskName = null)
        {
            return AddTask(TimerType.FIXED_REALTIME_DURATION, Time.realtimeSinceStartup + startDelay, interval, repeatTimes, executable, taskName);
        }

        /// <summary>
        /// Adds the frame execute task.
        /// </summary>
        /// <param name="executable">Executable.</param>
        public TimerTask AddFrameExecuteTask(Func<bool> executable, string taskName = null)
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
        private TimerTask AddTask(TimerType timerType, float startTime, float interval, int repeatTimes, Func<bool> executable, string taskName)
        {
            TimerTask task = ReferencePool.SpawnInstance<TimerTask>();
            task.timerType = timerType;
            task.startTime = startTime;
            task.interval = interval;
            task.repeatTimes = repeatTimes;
            task.executable = executable;

            // TimerTask task = new TimerTask { timerType = timerType, startTime = startTime, interval = interval, repeatTimes = repeatTimes, executable = executable };
#if UNITY_EDITOR
            task.name = string.IsNullOrEmpty(taskName) ? DefaultTaskName(executable) : taskName;
#endif
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
}
