namespace BenchmarkDotNet.Mathematics
{
    /// <summary>
    /// The enum is design to remove some outliers from the distribution.
    /// </summary>
    public enum OutlierMode
    {
        /// <summary>
        /// Don't remove outliers.
        /// </summary>
        None,
        
        /// <summary>
        /// Remove only upper outliers (which is bigger than upperFence).
        /// </summary>
        OnlyUpper,
        
        /// <summary>
        /// Remove only lower outliers (which is smaller than lowerFence).
        /// </summary>
        OnlyLower,
        
        /// <summary>
        /// Remove all outliers.
        /// </summary>
        All
    }
}