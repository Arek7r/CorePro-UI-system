using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CorePro.DictionaryPro
{
    [Serializable]
    public class DictionaryPro<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        // Internal dictionary for storing data
        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        [SerializeField]
        private List<TKey> keys = new List<TKey>();

        [SerializeField]
        private List<TValue> values = new List<TValue>();

        // IDictionary interface implementation
        public TValue this[TKey key]
        {
            get => dictionary[key];
            set => dictionary[key] = value;
        }

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(dictionary[item.Key], item.Value);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return dictionary.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        // Serialization methods
        public void OnBeforeSerialize()
        {
            // Rebuild serialized lists from the runtime dictionary, but preserve any existing
            // "pending" rows that have a null key - these are rows added via the Inspector drawer
            // before the user has assigned a key object. Clearing them would cause the row to
            // disappear immediately after clicking "+".

            // Collect all currently-pending (null-key) rows so we can re-append them afterwards.
            int pendingCount = 0;
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == null)
                    pendingCount++;
            }

            // Collect pending values (aligned with null keys) before clearing.
            TValue[] pendingValues = pendingCount > 0 ? new TValue[pendingCount] : null;
            if (pendingCount > 0)
            {
                int p = 0;
                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys[i] == null)
                        pendingValues[p++] = values[i];
                }
            }

            keys.Clear();
            values.Clear();

            foreach (var kvp in dictionary)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }

            // Re-append null-key pending rows so the Inspector drawer keeps them visible.
            if (pendingCount > 0)
            {
                for (int i = 0; i < pendingCount; i++)
                {
                    keys.Add(default);        // null for reference types
                    values.Add(pendingValues[i]);
                }
            }
        }

        public void OnAfterDeserialize()
        {
            dictionary.Clear();

            if (keys.Count != values.Count)
            {
                Debug.LogError($"DictionaryPro<{typeof(TKey).Name}, {typeof(TValue).Name}>: Mismatch between keys ({keys.Count}) and values ({values.Count}) count during deserialization. Data may be corrupted. Skipping deserialization.");
                return;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                // Skip null keys - happens when a new entry is added in the Inspector
                // before a key is assigned (ItemData slot left empty).
                if (keys[i] == null)
                    continue;

                if (!dictionary.ContainsKey(keys[i]))
                {
                    dictionary.Add(keys[i], values[i]);
                }
                else
                {
                    Debug.LogWarning($"DictionaryPro<{typeof(TKey).Name}, {typeof(TValue).Name}>: Duplicate key found during deserialization: {keys[i]}. Skipping duplicate.");
                }
            }
        }
    }
}