using System.Collections.Generic;

namespace Rk.Noise
{
    public interface INoise2d
    {
        /// <summary>
        /// Get the number of discrete values in the x dimension.
        /// </summary>
        uint Width { get; }

        /// <summary>
        /// Get the number of discrete values in the y dimension.
        /// </summary>
        uint Height { get; }

        /// <summary>
        /// Get the value at the coordinates (x, y).
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        float this[uint x, uint y] { get; }

        /// <summary>
        /// Create a 2D array from the values of the 2D noise.
        /// </summary>
        /// <returns></returns>
        float[,] ToArray2d();

        /// <summary>
        /// Create a 2D array where all values below the given value
        /// are unchanged, and values above are clamped to the given value.
        /// </summary>
        /// <param name="value">The value to which all greater values 
        /// are clamped.</param>
        /// <returns></returns>
        float[,] ToArray2dClampedTop(float value);

        /// <summary>
        /// Create a 2D array where all values above the given value
        /// are unchanged, and values below are clamped to the given value.
        /// </summary>
        /// <param name="value">The value to which all greater values 
        /// are clamped.</param>
        /// <returns></returns>
        float[,] ToArray2dClampedBottom(float value);

        /// <summary>
        /// Create a 2D array where for each given interval all values within that
        /// interval are set to the same value, which is associated with that interval.
        /// For each interval the upper bound is specified explicitly through the key of the 
        /// dictionary, and the lower bound is assumed implicitly as either the
        /// upper bound of the previous interval, or 0 in case of the first interval.
        /// The highest interval automatically includes all values up to the ValueNoise's
        /// maximum value, no matter whether it ends above or below 1. Intervals starting
        /// above 1 will be ignored.
        /// </summary>
        /// <param name="targetValueByIntervalUpperBound">The upper bound for each interval (key)
        /// and the target value for that interval (value).</param>
        /// <returns></returns>
        float[,] ToArray2dDiscretized(IDictionary<float, float> targetValueByIntervalUpperBound);
    }
}
