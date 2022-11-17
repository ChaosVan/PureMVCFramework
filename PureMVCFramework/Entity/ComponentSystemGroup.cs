#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace PureMVCFramework.Entity
{
    internal struct UpdateIndex
    {
        private ushort Data;

        public bool IsManaged => (Data & 0x8000) != 0;
        public int Index => Data & 0x7fff;

        public UpdateIndex(int index, bool managed)
        {
            Data = (ushort)index;
            Data |= (ushort)((managed ? 1 : 0) << 15);
        }

        override public string ToString()
        {
            return IsManaged ? "Managed: Index " + Index : "UnManaged: Index " + Index;
        }
    }

    /// <summary>
    /// The specified Type must be a ComponentSystemGroup.
    /// Updating in a group means this system will be automatically updated by the specified ComponentSystemGroup when the group is updated.
    /// The system may order itself relative to other systems in the group with UpdateBefore and UpdateAfter. This ordering takes
    /// effect when the system group is sorted.
    ///
    /// If the optional OrderFirst parameter is set to true, this system will act as if it has an implicit [UpdateBefore] targeting all other
    /// systems in the group that do *not* have OrderFirst=true, but it may still order itself relative to other systems with OrderFirst=true.
    ///
    /// If the optional OrderLast parameter is set to true, this system will act as if it has an implicit [UpdateAfter] targeting all other
    /// systems in the group that do *not* have OrderLast=true, but it may still order itself relative to other systems with OrderLast=true.
    ///
    /// An UpdateInGroup attribute with both OrderFirst=true and OrderLast=true is invalid, and will throw an exception.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class UpdateInGroupAttribute : Attribute
    {
        public bool OrderFirst = false;
        public bool OrderLast = false;

        public UpdateInGroupAttribute(Type groupType)
        {
            if (groupType == null)
                throw new ArgumentNullException(nameof(groupType));

            GroupType = groupType;
        }

        public Type GroupType { get; }
    }

    public abstract class ComponentSystemGroup : ComponentSystem
    {
        private bool m_systemSortDirty = false;

        // If true (the default), calling SortSystems() will sort the system update list, respecting the constraints
        // imposed by [UpdateBefore] and [UpdateAfter] attributes. SortSystems() is called automatically during
        // DefaultWorldInitialization, as well as at the beginning of ComponentSystemGroup.OnUpdate(), but may also be
        // called manually.
        //
        // If false, calls to SortSystems() on this system group will have no effect on update order of systems in this
        // group (though SortSystems() will still be called recursively on any child system groups). The group's systems
        // will update in the order of the most recent sort operation, with any newly-added systems updating in
        // insertion order at the end of the list.
        //
        // Setting this value to false is not recommended unless you know exactly what you're doing, and you have full
        // control over the systems which will be updated in this group.
        private bool m_EnableSystemSorting = true;
        public bool EnableSystemSorting
        {
            get => m_EnableSystemSorting;
            protected set
            {
                if (value && !m_EnableSystemSorting)
                    m_systemSortDirty = true; // force a sort after re-enabling sorting
                m_EnableSystemSorting = value;
            }
        }

        public bool Created { get; private set; } = false;

#if ODIN_INSPECTOR
        [ShowIf("showOdinInfo"), ShowInInspector, ListDrawerSettings(IsReadOnly = true)]
#endif
        internal List<ComponentSystemBase> m_systemsToUpdate = new List<ComponentSystemBase>();
        internal List<ComponentSystemBase> m_systemsToRemove = new List<ComponentSystemBase>();

        internal List<UpdateIndex> m_MasterUpdateList;

        public virtual IReadOnlyList<ComponentSystemBase> Systems => m_systemsToUpdate;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_MasterUpdateList = new List<UpdateIndex>(0);
            Created = true;
        }

        protected override void OnDestroy()
        {
            m_MasterUpdateList.Clear();
            base.OnDestroy();
            Created = false;
        }

        private void CheckCreated()
        {
            if (!Created)
                throw new InvalidOperationException($"Group of type {GetType()} has not been created, either the derived class forgot to call base.OnCreate(), or it has been destroyed");
        }

        public void AddSystemToUpdateList(ComponentSystemBase sys)
        {
            CheckCreated();

            if (sys != null)
            {
                if (this == sys)
                    throw new ArgumentException($"Can't add {TypeManager.GetSystemName(GetType())} to its own update list");

                // Check for duplicate Systems. Also see issue #1792
                if (m_systemsToUpdate.IndexOf(sys) >= 0)
                {
                    if (m_systemsToRemove.Contains(sys))
                    {
                        m_systemsToRemove.Remove(sys);
                    }
                    return;
                }

                m_MasterUpdateList.Add(new UpdateIndex(m_systemsToUpdate.Count, true));
                m_systemsToUpdate.Add(sys);
                m_systemSortDirty = true;
            }
        }

        private void RemovePending()
        {
            if (m_systemsToRemove.Count > 0)
            {
                foreach (var sys in m_systemsToRemove)
                {
                    m_systemsToUpdate.Remove(sys);
                }

                m_systemsToRemove.Clear();
            }
        }

        private void RemoveSystemsFromUnsortedUpdateList()
        {
            if (m_systemsToRemove.Count <= 0 /*&& m_UnmanagedSystemsToRemove.Length <= 0*/)
                return;

            //var world = World.Unmanaged;
            int largestID = 0;

            //determine the size of the lookup table used for looking up system information; whether a system is due to be removed
            //and/or the new update index of the system
            foreach (var managedSystem in m_systemsToUpdate)
            {
                largestID = math.max(largestID, managedSystem.CheckedState().m_SystemID);
            }

            var newListIndices = new NativeArray<int>(largestID + 1, Allocator.Temp);
            var systemIsRemoved = new NativeArray<byte>(largestID + 1, Allocator.Temp, NativeArrayOptions.ClearMemory);

            //update removed system lookup table
            foreach (var managedSystem in m_systemsToRemove)
            {
                systemIsRemoved[managedSystem.CheckedState().m_SystemID] = 1;
            }

            var newManagedUpdateList = new List<ComponentSystemBase>(m_systemsToUpdate.Count);
            //var newUnmanagedUpdateList = new UnsafeList<SystemHandleUntyped>(m_UnmanagedSystemsToUpdate.Length, Allocator.Persistent);

            //use removed lookup table to determine which systems will be in the new update
            foreach (var managedSystem in m_systemsToUpdate)
            {
                var systemID = managedSystem.CheckedState().m_SystemID;
                if (systemIsRemoved[systemID] == 0)
                {
                    //the new update index will be based on the position in the systems list
                    newListIndices[systemID] = newManagedUpdateList.Count;
                    newManagedUpdateList.Add(managedSystem);
                }
            }

            var newMasterUpdateList = new List<UpdateIndex>(newManagedUpdateList.Count);

            foreach (var updateIndex in m_MasterUpdateList)
            {
                if (updateIndex.IsManaged)
                {
                    var system = m_systemsToUpdate[updateIndex.Index];
                    var systemID = system.CheckedState().m_SystemID;
                    //use the two lookup tables to determine if and where the new master update list entries go
                    if (systemIsRemoved[systemID] == 0)
                    {
                        newMasterUpdateList.Add(new UpdateIndex(newListIndices[systemID], true));
                    }
                }
            }

            newListIndices.Dispose();
            systemIsRemoved.Dispose();

            m_systemsToUpdate = newManagedUpdateList;
            m_systemsToRemove.Clear();

            m_MasterUpdateList.Clear();
            m_MasterUpdateList = newMasterUpdateList;
        }

        private void RecurseUpdate()
        {
            if (!EnableSystemSorting)
            {
                RemoveSystemsFromUnsortedUpdateList();
            }
            else if (m_systemSortDirty)
            {
                GenerateMasterUpdateList();
            }
            m_systemSortDirty = false;

            foreach (var sys in m_systemsToUpdate)
            {
                if (TypeManager.IsSystemAGroup(sys.GetType()))
                {
                    var childGroup = sys as ComponentSystemGroup;
                    childGroup.RecurseUpdate();
                }
            }
        }

        private void GenerateMasterUpdateList()
        {
            RemovePending();

            var groupType = GetType();
            var allElems = new ComponentSystemSorter.SystemElement[m_systemsToUpdate.Count /*+ m_UnmanagedSystemsToUpdate.Length*/];
            var systemsPerBucket = new int[3];
            for (int i = 0; i < m_systemsToUpdate.Count; ++i)
            {
                var system = m_systemsToUpdate[i];
                var sysType = system.GetType();
                int orderingBucket = ComputeSystemOrdering(sysType, groupType);
                allElems[i] = new ComponentSystemSorter.SystemElement
                {
                    Type = sysType,
                    Index = new UpdateIndex(i, true),
                    OrderingBucket = orderingBucket,
                    updateBefore = new List<Type>(),
                    nAfter = 0,
                };
                systemsPerBucket[orderingBucket]++;
            }

            // Find & validate constraints between systems in the group
            ComponentSystemSorter.FindConstraints(groupType, allElems);

            // Build three lists of systems
            var elemBuckets = new[]
            {
                new ComponentSystemSorter.SystemElement[systemsPerBucket[0]],
                new ComponentSystemSorter.SystemElement[systemsPerBucket[1]],
                new ComponentSystemSorter.SystemElement[systemsPerBucket[2]],
            };
            var nextBucketIndex = new int[3];

            for (int i = 0; i < allElems.Length; ++i)
            {
                int bucket = allElems[i].OrderingBucket;
                int index = nextBucketIndex[bucket]++;
                elemBuckets[bucket][index] = allElems[i];
            }
            // Perform the sort for each bucket.
            for (int i = 0; i < 3; ++i)
            {
                if (elemBuckets[i].Length > 0)
                {
                    ComponentSystemSorter.Sort(elemBuckets[i]);
                }
            }

            // Because people can freely look at the list of managed systems, we need to put that part of list in order.
            var oldSystems = m_systemsToUpdate;
            m_systemsToUpdate = new List<ComponentSystemBase>(oldSystems.Count);
            for (int i = 0; i < 3; ++i)
            {
                foreach (var e in elemBuckets[i])
                {
                    var index = e.Index;
                    if (index.IsManaged)
                    {
                        m_systemsToUpdate.Add(oldSystems[index.Index]);
                    }
                }
            }

            // Commit results to master update list
            m_MasterUpdateList.Clear();
            m_MasterUpdateList.Capacity = allElems.Length;

            // Append buckets in order, but replace managed indices with incrementing indices
            // into the newly sorted m_systemsToUpdate list
            int managedIndex = 0;
            for (int i = 0; i < 3; ++i)
            {
                foreach (var e in elemBuckets[i])
                {
                    if (e.Index.IsManaged)
                    {
                        m_MasterUpdateList.Add(new UpdateIndex(managedIndex++, true));
                    }
                    else
                    {
                        m_MasterUpdateList.Add(e.Index);
                    }
                }
            }
        }

        internal static int ComputeSystemOrdering(Type sysType, Type ourType)
        {
            foreach (var uga in TypeManager.GetSystemAttributes(sysType, typeof(UpdateInGroupAttribute)))
            {
                var updateInGroupAttribute = (UpdateInGroupAttribute)uga;

                if (updateInGroupAttribute.GroupType.IsAssignableFrom(ourType))
                {
                    if (updateInGroupAttribute.OrderFirst)
                    {
                        return 0;
                    }

                    if (updateInGroupAttribute.OrderLast)
                    {
                        return 2;
                    }
                }
            }

            return 1;
        }

        /// <summary>
        /// Update the component system's sort order.
        /// </summary>
        public void SortSystems()
        {
            CheckCreated();

            RecurseUpdate();
        }

        protected override void OnStopRunning()
        {

        }

        internal override void OnStopRunningInternal()
        {
            OnStopRunning();

            foreach (var sys in m_systemsToUpdate)
            {
                if (sys == null)
                    continue;

                if (!sys.m_State.Enabled)
                    continue;

                if (!sys.m_State.PreviouslyEnabled)
                    continue;

                sys.m_State.PreviouslyEnabled = false;
                sys.OnStopRunningInternal();
            }
        }

        public IRateManager RateManager { get; set; }

        protected override void OnUpdate()
        {
            CheckCreated();

            if (RateManager == null)
            {
                UpdateAllSystems();
            }
            else
            {
                while (RateManager.ShouldGroupUpdate(this))
                {
                    UpdateAllSystems();
                }
            }
        }

        void UpdateAllSystems()
        {
            if (m_systemSortDirty)
                SortSystems();

            var world = World.Unmanaged;
            var previouslyExecutingSystem = world.ExecutingSystem;

            // Cache the update list length before updating; any new systems added mid-loop will change the length and
            // should not be processed until the subsequent group update, to give SortSystems() a chance to run.
            int updateListLength = m_MasterUpdateList.Count;
            for (int i = 0; i < updateListLength; ++i)
            {
                try
                {
                    var index = m_MasterUpdateList[i];

                    if (!index.IsManaged)
                    {
                        // TODO
                        throw new Exception();
                    }
                    else
                    {
                        // Update managed code.
                        var sys = m_systemsToUpdate[index.Index];
                        sys.Update();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                finally
                {
                    world.ExecutingSystem = previouslyExecutingSystem;
                }

                if (World.QuitUpdate)
                    break;
            }
        }
    }
}
