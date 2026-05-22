using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

namespace CorePro.Utils.Extensions
{
    public static class TransformExtensions
    {
        #region Setters/Getters: Position

        public static void SetX(this Transform t, float value)
        {
            var pos = t.position;
            pos.x = value;
            t.position = pos;
        }
        public static void SetY(this Transform t, float value)
        {
            var pos = t.position;
            pos.y = value;
            t.position = pos;
        }
        public static void SetZ(this Transform t, float value)
        {
            var pos = t.position;
            pos.z = value;
            t.position = pos;
        }
        public static void SetLocalX(this Transform t, float value)
        {
            var pos = t.localPosition;
            pos.x = value;
            t.localPosition = pos;
        }
        public static void SetLocalY(this Transform t, float value)
        {
            var pos = t.localPosition;
            pos.y = value;
            t.localPosition = pos;
        }
        public static void SetLocalZ(this Transform t, float value)
        {
            var pos = t.localPosition;
            pos.z = value;
            t.localPosition = pos;
        }

        public static void SetPosition(this Transform t, Vector3 value) => t.position = value;
        public static void SetLocalPosition(this Transform t, Vector3 value) => t.localPosition = value;
        public static Vector3 GetPosition(this Transform t) => t.position;
        public static Vector3 GetLocalPosition(this Transform t) => t.localPosition;

        #endregion

        #region Setters/Getters: Rotation

        public static void SetRotation(this Transform t, Quaternion value) => t.rotation = value;
        public static void SetLocalRotation(this Transform t, Quaternion value) => t.localRotation = value;
        public static Quaternion GetRotation(this Transform t) => t.rotation;
        public static Quaternion GetLocalRotation(this Transform t) => t.localRotation;

        public static void SetEulerAngles(this Transform t, Vector3 value) => t.eulerAngles = value;
        public static void SetLocalEulerAngles(this Transform t, Vector3 value) => t.localEulerAngles = value;
        public static Vector3 GetEulerAngles(this Transform t) => t.eulerAngles;
        public static Vector3 GetLocalEulerAngles(this Transform t) => t.localEulerAngles;

        /// <summary>
        /// Sets the world space rotation on the Y axis only.
        /// </summary>
        /// <param name="angle">The target angle in degrees.</param>
        public static void SetRotationY(this Transform transform, float angle)
        {
            Vector3 currentEuler = transform.eulerAngles;
            currentEuler.y = angle;
            transform.eulerAngles = currentEuler;
        }

        /// <summary>
        /// Sets the local space rotation on the Y axis only.
        /// </summary>
        /// <param name="angle">The target local angle in degrees.</param>
        public static void SetLocalRotationY(this Transform transform, float angle)
        {
            Vector3 currentLocalEuler = transform.localEulerAngles;
            currentLocalEuler.y = angle;
            transform.localEulerAngles = currentLocalEuler;
        }

        /// <summary>
        /// Sets the world space rotation on the Y axis using a Quaternion for better performance in some cases.
        /// </summary>
        public static void SetRotationYQuaternion(this Transform transform, float angle)
        {
            transform.rotation = Quaternion.Euler(transform.eulerAngles.x, angle, transform.eulerAngles.z);
        }
        #endregion

        #region Setters/Getters: Scale

        public static void SetScale(this Transform t, Vector3 value) => t.localScale = value;
        public static void SetUniformScale(this Transform t, float uniform) => t.localScale = new Vector3(uniform, uniform, uniform);
        public static Vector3 GetScale(this Transform t) => t.localScale;

        #endregion

        #region Parenting

        public static void SetParentAndReset(this Transform t, Transform parent)
        {
            t.SetParent(parent, false);
            t.Reset();
        }
        public static void SetParentAndKeepWorld(this Transform t, Transform parent)
        {
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;
            t.SetParent(parent, true);
            t.position = worldPos;
            t.rotation = worldRot;
        }

        #endregion

        #region Reset

        /// <summary>
        /// Reset local position, rotation and scale to (0,0,0)+(0,0,0)+(1,1,1)
        /// </summary>
        public static void Reset(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
        }

        /// <summary>
        /// For UI: Reset RectTransform local position, rotation, scale and anchors
        /// </summary>
        public static void ResetRect(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            if (t is RectTransform rect)
                rect.anchoredPosition = Vector2.zero;
        }

        #endregion

        #region Find/Hierarchy

        /// <summary>
        /// Returns Transform by HumanBodyBones if Animator attached, else null
        /// </summary>
        public static Transform FindTransform(this Transform t, HumanBodyBones bone)
        {
            var animator = t.GetComponent<Animator>();
            return animator != null ? animator.GetBoneTransform(bone) : null;
        }

        /// <summary>
        /// Recursively search by name (case-insensitive)
        /// </summary>
        public static Transform FindTransform(this Transform t, string name)
        {
            return FindChildTransform(t, name);
        }

        public static Transform FindChildTransform(Transform parent, string name)
        {
            string parentName = parent.name;
            if (string.Compare(parentName, name, true) == 0) return parent;

            // Handle namespaces, e.g. mixamo:Hips
            int idx = parentName.IndexOf(':');
            if (idx >= 0)
            {
                parentName = parentName.Substring(idx + 1);
                if (string.Compare(parentName, name, true) == 0) return parent;
            }
            for (int i = 0; i < parent.childCount; ++i)
            {
                var tChild = FindChildTransform(parent.GetChild(i), name);
                if (tChild != null) return tChild;
            }
            return null;
        }

        /// <summary>
        /// Finds chain from named transform, always first child to leaf
        /// </summary>
        public static void FindTransformChain(Transform parent, string name, ref List<Transform> result)
        {
            var t = parent.FindTransform(name);
            result.Clear();
            while (t != null)
            {
                result.Add(t);
                t = t.childCount > 0 ? t.GetChild(0) : null;
            }
        }

        /// <summary>
        /// Returns true if potentialAncestor is parent/grandparent/etc of t (or the same)
        /// </summary>
        public static bool IsChildOf(this Transform t, Transform potentialAncestor)
        {
            while (t != null)
            {
                if (t == potentialAncestor)
                    return true;
                t = t.parent;
            }
            return false;
        }

        /// <summary>
        /// Enumerates all children (direct only)
        /// </summary>
        public static IEnumerable<Transform> GetChildren(this Transform t)
        {
            for (int i = 0; i < t.childCount; ++i)
                yield return t.GetChild(i);
        }

        /// <summary>
        /// Enumerates all children, recursively
        /// </summary>
        public static IEnumerable<Transform> GetChildrenRecursive(this Transform t)
        {
            for (int i = 0; i < t.childCount; ++i)
            {
                var c = t.GetChild(i);
                yield return c;
                foreach (var d in GetChildrenRecursive(c))
                    yield return d;
            }
        }

        #endregion

        #region Utility / Space

        /// <summary>
        /// Moves towards a point (world), limited by maxDelta
        /// </summary>
        public static void MoveTowards(this Transform t, Vector3 target, float maxDelta)
        {
            t.position = Vector3.MoveTowards(t.position, target, maxDelta);
        }

        /// <summary>
        /// Rotates smoothly towards direction (world space)
        /// </summary>
        public static void RotateTowards(this Transform t, Vector3 direction, float speed)
        {
            if (direction == Vector3.zero)
                return;
            var targetRotation = Quaternion.LookRotation(direction.normalized);
            t.rotation = Quaternion.RotateTowards(t.rotation, targetRotation, speed * Time.deltaTime);
        }

        /// <summary>
        /// Sets local X axis in world space (rotation only)
        /// </summary>
        public static void SetWorldX(this Transform t, Vector3 worldX)
        {
            var rot = Quaternion.FromToRotation(t.right, worldX.normalized) * t.rotation;
            t.rotation = rot;
        }

        /// <summary>
        /// Returns distance to another Transform
        /// </summary>
        public static float DistanceTo(this Transform t, Transform other)
        {
            return Vector3.Distance(t.position, other.position);
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Set all children active or inactive (gameObject)
        /// </summary>
        public static void SetChildrenActive(this Transform t, bool active)
        {
            for (int i = 0; i < t.childCount; ++i)
                t.GetChild(i).gameObject.SetActive(active);
        }

        /// <summary>
        /// Destroys all children (immediate, be careful)
        /// </summary>
        public static void DestroyAllChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; --i)
            {
                var c = t.GetChild(i);
#if UNITY_EDITOR
                if (Application.isEditor && !Application.isPlaying)
                    Object.DestroyImmediate(c.gameObject);
                else
#endif
                    Object.Destroy(c.gameObject);
            }
        }

        #endregion

        #region RectTransform Extras

        public static void SetAnchorMin(this Transform t, Vector2 anchorMin)
        {
            if (t is RectTransform rect)
                rect.anchorMin = anchorMin;
        }
        public static void SetAnchorMax(this Transform t, Vector2 anchorMax)
        {
            if (t is RectTransform rect)
                rect.anchorMax = anchorMax;
        }
        public static void SetAnchoredPosition(this Transform t, Vector2 anchoredPos)
        {
            if (t is RectTransform rect)
                rect.anchoredPosition = anchoredPos;
        }
        public static void SetPivot(this Transform t, Vector2 pivot)
        {
            if (t is RectTransform rect)
                rect.pivot = pivot;
        }

        #endregion
    }
}
