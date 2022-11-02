using PureMVCFramework.Advantages;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace PureMVCFramework.Entity
{
    public static partial class EntityManager
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

        public static Entity Create(GameObject gameObject, out EntityCommandBuffer commandBuffer)
        {
            EntityArchetype archetype = default;
            return Create(gameObject, archetype, out commandBuffer);
        }

        public static Entity Create(GameObject gameObject, EntityArchetype archetype, out EntityCommandBuffer commandBuffer)
        {
            Assert.IsNotNull(gameObject);

            if (!TryGetEntity(gameObject, out var entity))
            {
                entity = InternalCreate(GUID_COUNT++, archetype);
                OnGameObjectLoaded(entity, gameObject, out commandBuffer);
            }
            else
            {
                commandBuffer = ecbs_begin.CreateCommandBuffer();
                commandBuffer.AddComponentData(entity, archetype);
            }

            return entity;
        }

        internal static void OnGameObjectLoaded(Entity entity, GameObject go, out EntityCommandBuffer commandBuffer)
        {
            OnGameObjectUnloaded(entity);

#if UNITY_EDITOR
            go.name = go.name.Replace("(Spawn)", $"({entity.GUID})");
#endif

            entity.gameObject = go;
            GameObjectEntities[entity.gameObject] = entity;
            OnEntityGameObjectLoaded?.Invoke(entity.gameObject, entity);

            commandBuffer = ecbs_begin.CreateCommandBuffer();
            commandBuffer.UpdateGameObject(entity);
        }

        internal static void OnGameObjectUnloaded(Entity entity)
        {
            if (entity.gameObject != null)
            {
                OnEntityGameObjectDeleted?.Invoke(entity.gameObject);
                GameObjectEntities.Remove(entity.gameObject);
                entity.gameObject = null;
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
                        OnGameObjectLoaded(e, go, out _);
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
                        OnGameObjectLoaded(e, go, out _);
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

            ecbs_begin.CreateCommandBuffer().LoadGameObject(entity, assetPath, parent, callback, userdata);
        }

        public static void LoadGameObject(EntityData entity, string assetPath, Vector3 position, Quaternion rotation, Action<Entity, object> callback = null, object userdata = null)
        {
            if (IsDataMode)
                return;

            ecbs_begin.CreateCommandBuffer().LoadGameObject(entity, assetPath, position, rotation, callback, userdata);
        }

        public static void UnloadGameObject(EntityData data)
        {
            if (TryGetEntity(data, out var entity))
                UnloadGameObject(entity);
        }

        public static void UnloadGameObject(EntityData data, out GameObject gameObject)
        {
            gameObject = null;
            if (TryGetEntity(data, out var entity))
                UnloadGameObject(entity, out gameObject);
        }

        public static void UnloadGameObject(Entity entity)
        {
            UnloadGameObject(entity, out var gameObject);
            if (gameObject != null)
                gameObject.Recycle();
        }

        public static void UnloadGameObject(Entity entity, out GameObject gameObject)
        {
            gameObject = null;
            if (IsDataMode)
                return;

            if (entity.IsAlive)
            {
                gameObject = entity.gameObject;
                OnGameObjectUnloaded(entity);

                var commandBuffer = ecbs_end.CreateCommandBuffer();
                commandBuffer.UpdateGameObject(entity);
            }
        }

        public static T AddComponentObject<T>(Entity entity) where T : Component
        {
            if (entity.IsAlive && entity.gameObject != null)
            {
                var comp = entity.gameObject.AddComponent<T>();
                ecbs_begin.CreateCommandBuffer().UpdateGameObject(entity);

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
                ecbs_end.CreateCommandBuffer().UpdateGameObject(entity);
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
