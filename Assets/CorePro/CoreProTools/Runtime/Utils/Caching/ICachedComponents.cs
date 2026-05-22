namespace CorePro.CoreTools
{
    /// <summary>
    /// Interface for objects that can provide cached references for context menu commands.
    /// Implement this on components that want to avoid GetComponent calls.
    /// </summary
    public interface ICachedComponents
    {
        /// <summary>
        /// Get a cached component of type T. Returns null if not found.
        /// No GetComponent call - data must be cached in Awake/Start.
        /// </summary>
        T GetCachedComponent<T>() where T : class;
    }
}