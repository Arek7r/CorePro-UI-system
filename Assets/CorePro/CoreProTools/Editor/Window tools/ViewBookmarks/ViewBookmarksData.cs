using System.Collections.Generic;
using UnityEngine;

namespace CorePro.Editor
{
    // Container for camera state data
    [System.Serializable]
    public class ViewPoint
    {
        public string Name;
        public Vector3 Pivot;
        public Quaternion Rotation;
        public float Size;
    }

    // ScriptableObject to persist bookmarks in the project
    public class ViewBookmarksData : ScriptableObject
    {
        public List<ViewPoint> Views = new List<ViewPoint>();
    }
}