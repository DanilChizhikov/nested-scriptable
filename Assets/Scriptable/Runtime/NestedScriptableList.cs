using System;
using System.Collections.Generic;
using UnityEngine;

namespace MBSCore.Scriptable
{
    [Serializable]
    public sealed class NestedScriptableList<T> : List<T>, ISerializationCallbackReceiver
        where T : ScriptableObject
    {
        [SerializeField] private T[] items = Array.Empty<T>();
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            items = ToArray();
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();
            AddRange(items);
        }
    }
}