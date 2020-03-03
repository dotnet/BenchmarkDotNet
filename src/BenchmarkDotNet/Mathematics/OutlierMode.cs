using System;

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
        [Obsolete("Use DontRemoveOutliers")]
        None,

        /// <summary>
        /// Remove only upper outliers (which is bigger than upperFence).
        /// </summary>
        [Obsolete("Use RemoveUpperOutliers")]
        OnlyUpper,

        /// <summary>
        /// Remove only lower outliers (which is smaller than lowerFence).
        /// </summary>
        [Obsolete("Use RemoveUpperOutliers")]
        OnlyLower,

        /// <summary>
        /// Remove all outliers.
        /// </summary>
        [Obsolete("Use RemoveAllOutliers")]
        All,

        /// <summary>
        /// Don't remove outliers.
        /// </summary>
        DontRemove,

        /// <summary>
        /// Remove only upper outliers (which is bigger than upperFence).
        /// </summary>
        RemoveUpper,

        /// <summary>
        /// Remove only lower outliers (which is smaller than lowerFence).
        /// </summary>
        RemoveLower,

        /// <summary>
        /// Remove all outliers.
        /// </summary>
        RemoveAll
    }
}