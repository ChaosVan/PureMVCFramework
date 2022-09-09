using PureMVCFramework.Advantages;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework.Entity
{
    public partial class EntityManager
    {
        internal static readonly Dictionary<GameObject, Entity> GameObjectEntities = new Dictionary<GameObject, Entity>();

        internal static event Action<GameObject, Entity> OnEntityGameObjectLoaded;
        internal static event Action<GameObject> OnEntityGameObjectDeleted;

        public static bool IsDataMode { get; set; }

        public static void Draw(LocalToWorld matrix)
        {
            Debug.DrawLine(matrix.Position, matrix.Position + math.mul(matrix.Rotation, math.right()) * 1, Color.red);
            Debug.DrawLine(matrix.Position, matrix.Position + math.mul(matrix.Rotation, math.up()) * 1, Color.green);
            Debug.DrawLine(matrix.Position, matrix.Position + math.mul(matrix.Rotation, math.forward()) * 1, Color.blue);
        }

        public static bool TryGetEntity(GameObject obj, out Entity entity)
        {
            return GameObjectEntities.TryGetValue(obj, out entity);
        }

        public static Entity Create(GameObject gameObject)
        {
            EntityArchetype archetype = default;
            return Create(gameObject, archetype);
        }

        public static Entity Create(GameObject gameObject, EntityArchetype archetype)
        {
            Assert.IsNotNull(gameObject);

            if (!TryGetEntity(gameObject, out var entity))
            {
                entity = InternalCreate(GUID_COUNT++, archetype);
                OnGameObjectLoaded(entity, gameObject);
            }

            return entity;
        }

        private static void OnGameObjectLoaded(Entity entity, GameObject go)
        {
            if (go != null)
            {
                entity.gameObject = go;
#if UNITY_EDITOR
                go.name = go.name.Replace("(Spawn)", $"({entity.GUID})");
#endif

                BeginCommandBuffer.CreateCommandBuffer().UpdateGameObject(entity);
                GameObjectEntities[entity.gameObject] = entity;
                OnEntityGameObjectLoaded?.Invoke(entity.gameObject, entity);
            }
        }

        internal static void InternalLoadGameObject(EntityData entity, string assetPath, Transform parent = null, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            ulong guid = entity.index;
            AutoReleaseManager.Instance.LoadGameObjectAsync(assetPath, parent, (go, data) =>
            {
                if (TryGetEntity(guid, out var e) && e.IsAlive)
                {
                    if (go != null)
                    {
                        OnGameObjectLoaded(e, go);
                        callback?.Invoke(e, data);
                    }
                }
                else
                {
                    go.Recycle();
                }
            }, userdata);
        }

        internal static void InternalLoadGameObject(EntityData entity, string assetPath, Vector3 position, Quaternion rotation, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            ulong guid = entity.index;
            AutoReleaseManager.Instance.LoadGameObjectAsync(assetPath, position, rotation, (go, data) =>
            {
                if (TryGetEntity(guid, out var e) && e.IsAlive)
                {
                    if (go != null)
                    {
                        OnGameObjectLoaded(e, go);
                        callback?.Invoke(e, data);
                    }
                }
                else
                {
                    go.Recycle();
                }
            }, userdata);
        }

        public static void LoadGameObject(EntityData entity, string assetPath, Transform parent = null, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            BeginCommandBuffer.CreateCommandBuffer().LoadGameObject(entity, assetPath, parent, callback, userdata);
        }

        public static void LoadGameObject(EntityData entity, string assetPath, Vector3 position, Quaternion rotation, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            BeginCommandBuffer.CreateCommandBuffer().LoadGameObject(entity, assetPath, position, rotation, callback, userdata);
        }

        public static T AddComponentObject<T>(Entity entity) where T : Component
        {
            if (entity.IsAlive && entity.gameObject != null)
            {
                var comp = entity.gameObject.AddComponent<T>();
                BeginCommandBuffer.CreateCommandBuffer().UpdateGameObject(entity);

                return comp;
            }

            return null;
        }

        public static void RemoveComponentObject<T>(Entity entity) where T : Component
        {
            var comp = GetComponentObject<T>(entity);
            if (comp != null)
            {
                UnityEngine.Object.Destroy(comp);
                EndCommandBuffer.CreateCommandBuffer().UpdateGameObject(entity);
            }
        }

        public static T GetComponentObject<T>(Entity entity) where T : Component
        {
            if (entity.IsAlive && entity.gameObject != null)
                return entity.gameObject.GetComponent<T>();

            return null;
        }
    }
}
