// 代码是由GenerateHybridSystem自动生成的，不要随意改动
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace PureMVCFramework.Entity
{
	public abstract class HybridSystem<T1, T2> : SystemBase where T1 : Component where T2 : IComponent 
	{
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T1> Components1 = new List<T1>();
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T2> Components2 = new List<T2>();
		private long hash2;
		public override void OnInitialized(params object[] args)
		{
			base.OnInitialized(args);
			hash2 = Entity.StringToHash(typeof(T2).FullName);
		}
		public override void OnRecycle()
		{
			Components1.Clear();
			Components2.Clear();
			base.OnRecycle();
		}
		public sealed override void InjectEntity(Entity entity)
		{
			if (entity.gameObject == null)
			{
				if (Entities.Contains(entity))
				{
					var i = Entities.IndexOf(entity);
					Entities.RemoveAt(i);
					Components1.RemoveAt(i);
					Components2.RemoveAt(i);
				}
				return;
			}
			var co = entity.gameObject.GetComponent<T1>();
			IComponent[] c = new IComponent[1];
			bool tf = co && entity.components.TryGetValue(hash2, out c[0]);
			if (Entities.Contains(entity))
			{
				if (!tf)
				{
					var i = Entities.IndexOf(entity);
					Entities.RemoveAt(i);
					Components1.RemoveAt(i);
					Components2.RemoveAt(i);
					OnEject(entity, co);
				}
			}
			else if (tf)
			{
				Entities.Add(entity);
				Components1.Add(co);
				Components2.Add((T2)c[0]);
			}
		}
		public sealed override void Update()
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				OnUpdate(i, Entities[i], Components1[i], Components2[i]);
			}
		}
		protected abstract void OnUpdate(int index, Entity entity, T1 component1, T2 component2);
		protected virtual void OnEject(Entity entity, T1 component) { }
	}
	public abstract class HybridSystem<T1, T2, T3> : SystemBase where T1 : Component where T2 : IComponent where T3 : IComponent 
	{
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T1> Components1 = new List<T1>();
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T2> Components2 = new List<T2>();
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T3> Components3 = new List<T3>();
		private long hash2;
		private long hash3;
		public override void OnInitialized(params object[] args)
		{
			base.OnInitialized(args);
			hash2 = Entity.StringToHash(typeof(T2).FullName);
			hash3 = Entity.StringToHash(typeof(T3).FullName);
		}
		public override void OnRecycle()
		{
			Components1.Clear();
			Components2.Clear();
			Components3.Clear();
			base.OnRecycle();
		}
		public sealed override void InjectEntity(Entity entity)
		{
			if (entity.gameObject == null)
			{
				if (Entities.Contains(entity))
				{
					var i = Entities.IndexOf(entity);
					Entities.RemoveAt(i);
					Components1.RemoveAt(i);
					Components2.RemoveAt(i);
					Components3.RemoveAt(i);
				}
				return;
			}
			var co = entity.gameObject.GetComponent<T1>();
			IComponent[] c = new IComponent[2];
			bool tf = co && entity.components.TryGetValue(hash2, out c[0]) && entity.components.TryGetValue(hash3, out c[1]);
			if (Entities.Contains(entity))
			{
				if (!tf)
				{
					var i = Entities.IndexOf(entity);
					Entities.RemoveAt(i);
					Components1.RemoveAt(i);
					Components2.RemoveAt(i);
					Components3.RemoveAt(i);
					OnEject(entity, co);
				}
			}
			else if (tf)
			{
				Entities.Add(entity);
				Components1.Add(co);
				Components2.Add((T2)c[0]);
				Components3.Add((T3)c[1]);
			}
		}
		public sealed override void Update()
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				OnUpdate(i, Entities[i], Components1[i], Components2[i], Components3[i]);
			}
		}
		protected abstract void OnUpdate(int index, Entity entity, T1 component1, T2 component2, T3 component3);
		protected virtual void OnEject(Entity entity, T1 component) { }
	}
	public abstract class HybridSystem<T1, T2, T3, T4> : SystemBase where T1 : Component where T2 : IComponent where T3 : IComponent where T4 : IComponent 
	{
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T1> Components1 = new List<T1>();
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T2> Components2 = new List<T2>();
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T3> Components3 = new List<T3>();
#if ODIN_INSPECTOR
		[ShowInInspector, ShowIf("showOdinInfo"), ListDrawerSettings(IsReadOnly = true)]
#endif
		private readonly List<T4> Components4 = new List<T4>();
		private long hash2;
		private long hash3;
		private long hash4;
		public override void OnInitialized(params object[] args)
		{
			base.OnInitialized(args);
			hash2 = Entity.StringToHash(typeof(T2).FullName);
			hash3 = Entity.StringToHash(typeof(T3).FullName);
			hash4 = Entity.StringToHash(typeof(T4).FullName);
		}
		public override void OnRecycle()
		{
			Components1.Clear();
			Components2.Clear();
			Components3.Clear();
			Components4.Clear();
			base.OnRecycle();
		}
		public sealed override void InjectEntity(Entity entity)
		{
			if (entity.gameObject == null)
			{
				if (Entities.Contains(entity))
				{
					var i = Entities.IndexOf(entity);
					Entities.RemoveAt(i);
					Components1.RemoveAt(i);
					Components2.RemoveAt(i);
					Components3.RemoveAt(i);
					Components4.RemoveAt(i);
				}
				return;
			}
			var co = entity.gameObject.GetComponent<T1>();
			IComponent[] c = new IComponent[3];
			bool tf = co && entity.components.TryGetValue(hash2, out c[0]) && entity.components.TryGetValue(hash3, out c[1]) && entity.components.TryGetValue(hash4, out c[2]);
			if (Entities.Contains(entity))
			{
				if (!tf)
				{
					var i = Entities.IndexOf(entity);
					Entities.RemoveAt(i);
					Components1.RemoveAt(i);
					Components2.RemoveAt(i);
					Components3.RemoveAt(i);
					Components4.RemoveAt(i);
					OnEject(entity, co);
				}
			}
			else if (tf)
			{
				Entities.Add(entity);
				Components1.Add(co);
				Components2.Add((T2)c[0]);
				Components3.Add((T3)c[1]);
				Components4.Add((T4)c[2]);
			}
		}
		public sealed override void Update()
		{
			for (int i = 0; i < Entities.Count; ++i)
			{
				OnUpdate(i, Entities[i], Components1[i], Components2[i], Components3[i], Components4[i]);
			}
		}
		protected abstract void OnUpdate(int index, Entity entity, T1 component1, T2 component2, T3 component3, T4 component4);
		protected virtual void OnEject(Entity entity, T1 component) { }
	}
}
