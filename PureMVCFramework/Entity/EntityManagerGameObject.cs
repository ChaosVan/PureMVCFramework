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
            UnityEngine.Debug.DrawLine(matrix.Position, matrix.Position + math.mul(matrix.Rotation, math.right()) * 1, UnityEngine.Color.red);
            UnityEngine.Debug.DrawLine(matrix.Position, matrix.Position + math.mul(matrix.Rotation, math.up()) * 1, UnityEngine.Color.green);
            UnityEngine.Debug.DrawLine(matrix.Position, matrix.Position + math.mul(matrix.Rotation, math.forward()) * 1, UnityEngine.Color.blue);
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

            if (!GameObjectEntities.TryGetValue(gameObject, out var entity))
            {
                entity = Create(archetype);
                OnGameObjectLoaded(entity, gameObject);
            }

            return entity;
        }

        private static void OnGameObjectLoaded(Entity entity, GameObject go)
        {
            if (go != null)
            {
                var array = BeginStructual(1, entity);
                entity.gameObject = go;
#if UNITY_EDITOR
                entity.name = go.name = go.name.Replace("(Spawn)", $"({entity.GUID})");
#endif
                EndStructual(array);

                GameObjectEntities[entity.gameObject] = entity;
                OnEntityGameObjectLoaded?.Invoke(entity.gameObject, entity);
            }
        }

        internal static void LoadGameObject(EntityData entity, string assetPath, Transform parent = null, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            ulong guid = entity.index;
            AutoReleaseManager.Instance.LoadGameObjectAsync(assetPath, parent, (go, data) =>
            {
                if (Entities.TryGetValue(guid, out var entity) && entity.IsAlive)
                {
                    if (go != null)
                    {
                        OnGameObjectLoaded(entity, go);
                        callback?.Invoke(entity, data);
                    }
                }
                else
                {
                    go.Recycle();
                }
            }, userdata);
        }

        public static void LoadGameObject(Entity entity, string assetPath, Transform parent = null, Action<Entity, object> callback = null, object userdata = null)
        {
            LoadGameObject(entity.GUID, assetPath, parent, callback, userdata);
        }

        internal static void LoadGameObject(EntityData entity, string assetPath, Vector3 position, Quaternion rotation, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            ulong guid = entity.index;
            AutoReleaseManager.Instance.LoadGameObjectAsync(assetPath, position, rotation, (go, data) =>
            {
                if (Entities.TryGetValue(guid, out var entity) && entity.IsAlive)
                {
                    if (go != null)
                    {
                        OnGameObjectLoaded(entity, go);
                        callback?.Invoke(entity, data);
                    }
                }
                else
                {
                    go.Recycle();
                }
            }, userdata);
        }

        public static void LoadGameObject(Entity entity, string assetPath, Vector3 position, Quaternion rotation, Action<Entity, object> callback = null, object userdata = null)
        {
            LoadGameObject(entity.GUID, assetPath, position, rotation, callback, userdata);
        }

        public static T AddComponentObject<T>(Entity entity) where T : Component
        {
            if (entity.IsAlive && entity.gameObject != null)
            {
                var entities = BeginStructual(1, entity);
                var comp = entity.gameObject.AddComponent<T>();
                EndStructual(entities);

                return comp;
            }

            return null;
        }

        public static T GetComponentObject<T>(Entity entity) where T : Component
        {
            if (entity.IsAlive && entity.gameObject != null)
                return entity.gameObject.GetComponent<T>();

            return null;
        }

        public static void RemoveComponentObject<T>(Entity entity) where T : Component
        {
            var comp = GetComponentObject<T>(entity);
            if (comp != null)
            {
                var entities = BeginStructual(1, entity);
                UnityEngine.Object.Destroy(comp);
                EndStructual(entities);
            }
        }
    }
}
