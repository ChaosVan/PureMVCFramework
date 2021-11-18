namespace PureMVCFramework.Entity
{
    [System.Serializable]
    public class World : VirtualWorld
    {
        public static World Self { get; private set; }

        public float DeltaTime { get; private set; }

        internal World() { }

        public override void Initialize()
        {
            Self = this;
        }

        public void OnUpdate(float delta)
        {
            DeltaTime = delta;
            int count = m_Systems.Count;

            for (int i = 0; i < count; ++i)
            {
                m_Systems[i].PreUpdate();
            }
            for (int i = 0; i < count; ++i)
            {
                m_Systems[i].Update();
            }
            for (int i = 0; i < count; ++i)
            {
                m_Systems[i].PostUpdate();
            }
        }
    }
}
