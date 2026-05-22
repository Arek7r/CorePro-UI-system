using System.Collections.Generic;
using UnityEngine;

namespace CorePro.Utils
{
    /// <summary>
    /// Helper class for efficient tag validation operations.
    /// Provides zero-allocation methods for checking tags against arrays or lists.
    /// 
    /// Usage examples:
    ///     if (validTags.ContainsTag(gameObject.tag)) { ... }
    ///     if (validTags.ContainsTag(collider)) { ... }
    ///     if (validTags.ContainsTag(transform)) { ... }
    /// </summary>
    public static class TagHelper
    {
        /// <summary>
        /// Checks if the tag array contains the specified tag string.
        /// Zero GC allocation.
        /// </summary>
        /// <param name="tags">Array of valid tags</param>
        /// <param name="tag">Tag to check</param>
        /// <returns>True if tag is found in array</returns>
        public static bool ContainsTag(this string[] tags, string tag)
        {
            if (tags == null || tags.Length == 0)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the tag array contains the GameObject's tag.
        /// Zero GC allocation.
        /// </summary>
        /// <param name="tags">Array of valid tags</param>
        /// <param name="gameObject">GameObject to check</param>
        /// <returns>True if GameObject's tag is found in array</returns>
        public static bool ContainsTag(this string[] tags, GameObject gameObject)
        {
            if (gameObject == null || tags == null || tags.Length == 0)
                return false;

            for (int i = 0; i < tags.Length; i++)
            {
                if (gameObject.CompareTag(tags[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the tag array contains the Collider's GameObject tag.
        /// Zero GC allocation.
        /// </summary>
        /// <param name="tags">Array of valid tags</param>
        /// <param name="collider">Collider to check</param>
        /// <returns>True if Collider's tag is found in array</returns>
        public static bool ContainsTag(this string[] tags, Collider collider)
        {
            if (collider == null)
                return false;

            return tags.ContainsTag(collider.gameObject);
        }

        /// <summary>
        /// Checks if the tag array contains the Transform's GameObject tag.
        /// Zero GC allocation.
        /// </summary>
        /// <param name="tags">Array of valid tags</param>
        /// <param name="transform">Transform to check</param>
        /// <returns>True if Transform's tag is found in array</returns>
        public static bool ContainsTag(this string[] tags, Transform transform)
        {
            if (transform == null)
                return false;

            return tags.ContainsTag(transform.gameObject);
        }

        /// <summary>
        /// Checks if tag array is empty or null (allows all tags).
        /// </summary>
        /// <param name="tags">Array of tags to check</param>
        /// <returns>True if array is null or empty</returns>
        public static bool IsEmpty(this string[] tags)
        {
            return tags == null || tags.Length == 0;
        }

        /// <summary>
        /// Checks if the tag array contains the specified tag, or returns true if array is empty (allow all).
        /// </summary>
        /// <param name="tags">Array of valid tags (null/empty = allow all)</param>
        /// <param name="tag">Tag to check</param>
        /// <returns>True if tag is found or array is empty</returns>
        public static bool ContainsTagOrEmpty(this string[] tags, string tag)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(tag);
        }

        /// <summary>
        /// Checks if the tag array contains the GameObject's tag, or returns true if array is empty (allow all).
        /// </summary>
        public static bool ContainsTagOrEmpty(this string[] tags, GameObject gameObject)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(gameObject);
        }

        /// <summary>
        /// Checks if the tag array contains the Collider's tag, or returns true if array is empty (allow all).
        /// </summary>
        public static bool ContainsTagOrEmpty(this string[] tags, Collider collider)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(collider);
        }

        /// <summary>
        /// Checks if the tag array contains the Transform's tag, or returns true if array is empty (allow all).
        /// </summary>
        public static bool ContainsTagOrEmpty(this string[] tags, Transform transform)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(transform);
        }

        /// <summary>
        /// Checks if the tag list contains the specified tag string.
        /// Zero GC allocation if list is not null and contains elements.
        /// </summary>
        /// <param name="tags">List of valid tags</param>
        /// <param name="tag">Tag to check</param>
        /// <returns>True if tag is found in list</returns>
        public static bool ContainsTag(this List<string> tags, string tag)
        {
            if (tags == null || tags.Count == 0)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (tags[i] == tag)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the tag list contains the GameObject's tag.
        /// Zero GC allocation if list is not null and contains elements.
        /// </summary>
        /// <param name="tags">List of valid tags</param>
        /// <param name="gameObject">GameObject to check</param>
        /// <returns>True if GameObject's tag is found in list</returns>
        public static bool ContainsTag(this List<string> tags, GameObject gameObject)
        {
            if (gameObject == null || tags == null || tags.Count == 0)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (gameObject.CompareTag(tags[i]))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the tag list contains the Collider's GameObject tag.
        /// Zero GC allocation if list is not null and contains elements.
        /// </summary>
        /// <param name="tags">List of valid tags</param>
        /// <param name="collider">Collider to check</param>
        /// <returns>True if Collider's tag is found in list</returns>
        public static bool ContainsTag(this List<string> tags, Collider collider)
        {
            if (collider == null)
                return false;

            return tags.ContainsTag(collider.gameObject);
        }

        /// <summary>
        /// Checks if the tag list contains the Transform's GameObject tag.
        /// Zero GC allocation if list is not null and contains elements.
        /// </summary>
        /// <param name="tags">List of valid tags</param>
        /// <param name="transform">Transform to check</param>
        /// <returns>True if Transform's tag is found in list</returns>
        public static bool ContainsTag(this List<string> tags, Transform transform)
        {
            if (transform == null)
                return false;

            return tags.ContainsTag(transform.gameObject);
        }

        /// <summary>
        /// Checks if tag list is empty or null (allows all tags).
        /// </summary>
        /// <param name="tags">List of tags to check</param>
        /// <returns>True if list is null or empty</returns>
        public static bool IsEmpty(this List<string> tags)
        {
            return tags == null || tags.Count == 0;
        }

        /// <summary>
        /// Checks if the tag list contains the specified tag, or returns true if list is empty (allow all).
        /// </summary>
        /// <param name="tags">List of valid tags (null/empty = allow all)</param>
        /// <param name="tag">Tag to check</param>
        /// <returns>True if tag is found or list is empty</returns>
        public static bool ContainsTagOrEmpty(this List<string> tags, string tag)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(tag);
        }

        /// <summary>
        /// Checks if the tag list contains the GameObject's tag, or returns true if list is empty (allow all).
        /// </summary>
        public static bool ContainsTagOrEmpty(this List<string> tags, GameObject gameObject)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(gameObject);
        }

        /// <summary>
        /// Checks if the tag list contains the Collider's tag, or returns true if list is empty (allow all).
        /// </summary>
        public static bool ContainsTagOrEmpty(this List<string> tags, Collider collider)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(collider);
        }

        /// <summary>
        /// Checks if the tag list contains the Transform's tag, or returns true if list is empty (allow all).
        /// </summary>
        public static bool ContainsTagOrEmpty(this List<string> tags, Transform transform)
        {
            if (tags.IsEmpty())
                return true;

            return tags.ContainsTag(transform);
        }
    }
}