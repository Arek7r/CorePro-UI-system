using UnityEngine;

namespace CorePro.Utils.Extensions
{
    public static class RigidBodyExtensions
    {
        public static void Reset(this Rigidbody rb)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.linearDamping = 0;

            if (rb.isKinematic == false)
                rb.angularVelocity = Vector3.zero;
    
            rb.inertiaTensorRotation = Quaternion.identity;
            rb.ResetInertiaTensor();
            rb.ResetCenterOfMass();
            
#if UNITY_6000_0_OR_NEWER
            if (rb.isKinematic == false)
                rb.linearVelocity = Vector3.zero;
#else   
            rb.moveSpeed = Vector3.zero;
#endif
            rb.isKinematic = !rb.isKinematic;
            rb.isKinematic = !rb.isKinematic;
            rb.constraints = RigidbodyConstraints.None;
        }
    }
}