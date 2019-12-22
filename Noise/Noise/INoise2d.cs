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
        float[,] ToArray2dClamped(float value);
    }
}
