namespace CorePro.Utils.Extensions
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public static partial class RaycastExtensions
    {
        /// <summary>
        /// Total number of hits that can happen with our pre-allocated arrays
        /// </summary>
        public const int MAX_HITS = 40;

        /// <summary>
        /// Used to compare hits based on distance
        /// </summary>
         public static RaycastHitDistanceComparer HitDistanceComparer = new RaycastHitDistanceComparer();

        /// <summary>
        /// Used when we need to return an empty raycast
        /// </summary>
        public static RaycastHit EmptyHitInfo = new RaycastHit();

        /// <summary>
        /// We use this if we don't want to re-allocate arrays. This is simple, but
        /// won't work with multi-threading and the contents need to be used immediately,
        /// as they are not persistant across alls.
        /// </summary>
        public static RaycastHit[] SharedHitArray = new RaycastHit[MAX_HITS];

        /// <summary>
        /// We use this if we don't want to re-allocate arrays. This is simple, but
        /// won't work with multi-threading and the contents need to be used immediately,
        /// as they are not persistant across alls.
        /// </summary>
        public static Collider[] SharedColliderArray = new Collider[MAX_HITS];

        
        /// <summary>
        /// Determines if the "descendant" transform is a child (or grand child)
        /// of the "parent" transform.
        /// </summary>
        /// <param name="rParent"></param>
        /// <param name="rTest"></param>
        /// <returns></returns>
        private static bool IsDescendant(Transform rParent, Transform rDescendant)
        {
            if (rParent == null) { return false; }

            Transform lDescendantParent = rDescendant;
            while (lDescendantParent != null)
            {
                if (lDescendantParent == rParent) { return true; }
                lDescendantParent = lDescendantParent.parent;
            }

            return false;
        }
        
        /// <summary>
        /// Comparerer for distance
        /// </summary>
        public class RaycastHitDistanceComparer : IComparer
        {
            int IComparer.Compare(object rCompare1, object rCompare2)
            {
                RaycastHit lCompare1 = (RaycastHit)rCompare1;
                RaycastHit lCompare2 = (RaycastHit)rCompare2;

                if (lCompare1.distance > lCompare2.distance) { return 1; }
                if (lCompare1.distance < lCompare2.distance) { return -1; }
                else { return 0; }
            }
        }
        
        // public static bool CheckForwardCollision(Vector3 start, Vector3 end, out RaycastHit hit, float extraDistance = 0, int layerMask = ~0)
        // {
        //     return CheckForwardCollision(start, end, out hit, extraDistance, layerMask);
        // }
        public static bool CheckForwardCollision(Vector3 start, Vector3 end, out RaycastHit hit, float extraDistance = 0, int layerMask = ~0, Collider ignoreCollider = null)
        {
                Vector3 direction = end - start;
                float distance = direction.magnitude + extraDistance;
                if (distance <= 0.0001f)
                {
                    hit = default;
                    return false;
                }

                int count = Physics.RaycastNonAlloc(start, direction.normalized, SharedHitArray, distance, layerMask, QueryTriggerInteraction.Ignore);

                for (int i = 0; i < count; i++)
                {
                    var h = SharedHitArray[i];
                    if (ignoreCollider != null && h.collider == ignoreCollider)
                        continue;

                    hit = h;
                    return true;
                }

                hit = default;
                return false;
        }
        
        public static bool CheckForwardCollision(Vector3 start, Vector3 end, out RaycastHit hit, float extraDistance = 0, int layerMask = ~0, ICollection<Collider> ignoreColliders = null)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude + extraDistance;
            if (distance <= 0.0001f)
            {
                hit = default;
                return false;
            }

            int count = Physics.RaycastNonAlloc(start, direction.normalized, SharedHitArray, distance, layerMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var h = SharedHitArray[i];
                if (ignoreColliders != null && ignoreColliders.Contains(h.collider))
                    continue;

                hit = h;
                return true;
            }

            hit = default;
            return false;
        }
        
        /// <summary>
        /// Use the non-alloc version of raycast to see if the ray hits anything. Here we are
        /// not particular about what we hit. We just test for a hit
        /// </summary>
        /// <param name="rPosition">Position of the sphere</param>
        /// <param name="rRadius">Radius of the sphere</param>
        /// <param name="rCollisionArray">Array of collision objects that were hit</param>
        /// <param name="rLayerMask">Layer mask to determine what we'll hit</param>
        /// <param name="rIgnore">Single transform we'll test if we should ignore</param>
        /// <param name="rIgnoreList">List of transforms we should ignore collisions with</param>
        /// <returns></returns>
        public static int SafeOverlapSphere(Vector3 rPosition, float rRadius, out Collider[] rColliderArray, int rLayerMask = -1, Transform rIgnore = null, List<Transform> rIgnoreList = null, bool rIgnoreTriggers = true)
        {
            rColliderArray = null;

            // Use the non allocating version
            int lHits = 0;

            if (rLayerMask != -1)
            {
                lHits = UnityEngine.Physics.OverlapSphereNonAlloc(rPosition, rRadius, SharedColliderArray, rLayerMask, (rIgnoreTriggers ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide));
            }
            else
            {
                lHits = UnityEngine.Physics.OverlapSphereNonAlloc(rPosition, rRadius, SharedColliderArray);
            }

            // With no hits, this is easy
            if (lHits == 0)
            {
                return 0;
            }
            // One hit is also easy
            else if (lHits == 1)
            {
                if (rIgnoreTriggers && SharedColliderArray[0].isTrigger) { return 0; }

                Transform lColliderTransform = SharedColliderArray[0].transform;

                if (rIgnore != null && IsDescendant(rIgnore, lColliderTransform)) { return 0; }

                if (rIgnoreList != null)
                {
                    for (int i = 0; i < rIgnoreList.Count; i++)
                    {
                        if (IsDescendant(rIgnoreList[i], lColliderTransform)) { return 0; }
                    }
                }

                rColliderArray = SharedColliderArray;
                return 1;
            }
            // Go through all the hits and see if any hit
            else
            {
                int lValidHits = 0;
                for (int i = 0; i < lHits; i++)
                {
                    bool lShift = false;
                    Transform lColliderTransform = SharedColliderArray[i].transform;

                    if (rIgnoreTriggers && SharedColliderArray[i].isTrigger) { lShift = true; }

                    if (rIgnore != null && IsDescendant(rIgnore, lColliderTransform)) { lShift = true; }

                    if (rIgnoreList != null)
                    {
                        for (int j = 0; j < rIgnoreList.Count; j++)
                        {
                            if (IsDescendant(rIgnoreList[j], lColliderTransform))
                            {
                                lShift = true;
                                break;
                            }
                        }
                    }

                    if (lShift)
                    {
                        // Move our index so when the for-loop iterates us forward, we stay put
                        lHits--;

                        // Shift the contents left, but we care about the old count (hence the + 1)
                        for (int j = i; j < lHits; j++)
                        {
                            SharedColliderArray[j] = SharedColliderArray[j + 1];
                        }

                        // Move our index so when the for-loop iterates us forward, we stay put
                        i--;
                    }
                    else
                    {
                        lValidHits++;
                    }
                }

                rColliderArray = SharedColliderArray;
                return lValidHits;
            }
        }
    }
}