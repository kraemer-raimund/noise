using System;
using System.Collections.Generic;
using System.Linq;

namespace Rk.Noise
{
    public class ValueNoise2d : INoise2d
    {
        private readonly float[,] _values;
        private readonly Random _random = new Random();
        private readonly Interpolation _interpolation;
        private readonly uint _waveLength;
        private readonly float _factor;

        /// <summary>
        /// Create a new 2D value noise.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="interpolation">The type of interpolation to use for noise generation.</param>
        /// <param name="waveLength">The distance between random values on the first octave, 
        /// which is the one with the lowest level of detail.</param>
        /// <param name="numberOfOctaves">The number of levels of detail, where the wavelength 
        /// gets halved for each additional octave. Octaves after ld(waveLength) will
        /// be omitted, since the minimum wavelength for the last octave is 1.</param>
        /// <param name="factor">The factor with which to multiply the values for the
        /// next octave with lower wavelength.</param>
        public ValueNoise2d(
            uint width,
            uint height,
            Interpolation interpolation,
            uint waveLength,
            uint numberOfOctaves,
            float factor = 0.5f)
        {
            Width = width;
            Height = height;
            _interpolation = interpolation;
            _waveLength = waveLength;
            _factor = factor;

            IEnumerable<float[,]> octaves = GenerateOctaves(numberOfOctaves);
            float[,] sumOfOctaves = SumMatrices(octaves);
            _values = NormalizeMatrix(sumOfOctaves);
        }

        public float this[uint x, uint y] => _values[x, y];

        public uint Width { get; }

        public uint Height { get; }

        public float[,] ToArray2d()
        {
            var copy = new float[Width, Height];
            _values.CopyTo(copy, _values.Length);
            return copy;
        }

        public float[,] ToArray2dClamped(float value)
        {
            var copy = new float[Width, Height];
            _values.CopyTo(copy, _values.Length);

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    if (copy[x, y] > value)
                    {
                        copy[x, y] = value;
                    }
                }
            }

            return copy;
        }

        /// <summary>
        /// Generate a lattice of random values in the interval [0, octaveMaxValue) 
        /// between which the noise values can be interpolated. Note that depending on
        /// the type of interpolation the generated values can actually be smaller or 
        /// greater than the minimum and maximum values of the lattice.
        /// </summary>
        /// <returns></returns>
        private float[,] GenerateRandomLattice(uint width, uint height, float octaveMaxValue)
        {
            var lattice = new float[width, height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    lattice[x, y] = (float)_random.NextDouble() * octaveMaxValue;
                }
            }

            return lattice;
        }

        private IEnumerable<float[,]> GenerateOctaves(uint amount)
        {
            if (amount < 1)
            {
                throw new System.ArgumentException("Number of octaves cannot be less than 1.");
            }

            var octaves = new List<float[,]>();

            for (var i = 0; i < amount; i++)
            {
                uint octaveWaveLength = (uint)(_waveLength * Math.Pow(0.5f, i));
                // Start at 1 for the first octave and multiply by the specified factor for each following octave.
                float octaveMaxValue = (float)Math.Pow(_factor, i);

                var octave = GenerateOctave(octaveWaveLength, octaveMaxValue);
                octaves.Add(octave);
            }

            return octaves;
        }

        private float[,] GenerateOctave(uint octaveWaveLength, float octaveMaxValue)
        {
            switch (_interpolation)
            {
                case Interpolation.Cubic:
                    return GenerateOctaveWithCubicInterpolation(octaveWaveLength, octaveMaxValue);
                case Interpolation.Linear:
                    return GenerateOctaveWithLinearInterpolation(octaveWaveLength, octaveMaxValue);
                default:
                    throw new InvalidOperationException("Interpolation type not implemented.");
            }
        }

        private float[,] GenerateOctaveWithCubicInterpolation(uint octaveWaveLength, float octaveMaxValue)
        {
            var octave = new float[Width, Height];

            // For Catmull-Rom cubic interpolation, we need one additional points on each side.
            // Depending on the parameters, we need to add up to 2 additional lattice points to 
            // cover the whole noise dimensions with the lattice, so we always add 4 for 
            // simplicity, which covers all cases.
            uint latticeWidth = (Width / octaveWaveLength) + 4;
            uint latticeHeight = (Height / octaveWaveLength) + 4;
            float[,] lattice = GenerateRandomLattice(latticeWidth, latticeHeight, octaveMaxValue);

            for (uint x = 0; x < Width; x++)
            {
                for (uint y = 0; y < Height; y++)
                {
                    // The offset of the current lattice quad, i. e. the rectangle of values surrounded
                    // by 4 lattice points where this point is the one out of these 4 that is furthest
                    // to the bottom left.
                    // Because of the additional lattice point on each side used for cubic interpolation,
                    // we need to offset it by 1 additionally.
                    uint latticeX = (x / octaveWaveLength) + 1;
                    uint latticeY = (y / octaveWaveLength) + 1;

                    // The 4 lattice points surrounding the segment of the noise, plus the additional 
                    // lattice points used for cubic interpolation, from which the intermediate noise 
                    // values can be generated.
                    var currentSegment = new float[4][]
                    {
                        new float[4] { lattice[latticeX - 1, latticeY - 1], lattice[latticeX, latticeY - 1], lattice[latticeX + 1, latticeY - 1], lattice[latticeX + 2, latticeY - 1] },
                        new float[4] { lattice[latticeX - 1, latticeY    ], lattice[latticeX, latticeY    ], lattice[latticeX + 1, latticeY    ], lattice[latticeX + 2, latticeY    ] },
                        new float[4] { lattice[latticeX - 1, latticeY + 1], lattice[latticeX, latticeY + 1], lattice[latticeX + 1, latticeY + 1], lattice[latticeX + 2, latticeY + 1] },
                        new float[4] { lattice[latticeX - 1, latticeY + 2], lattice[latticeX, latticeY + 2], lattice[latticeX + 1, latticeY + 2], lattice[latticeX + 2, latticeY + 2] },
                    };

                    // The offset of the current coordinate within the containing lattice quad.
                    float weightX = (float)(x % octaveWaveLength) / (octaveWaveLength + 1);
                    float weightY = (float)(y % octaveWaveLength) / (octaveWaveLength + 1);

                    octave[x, y] = InterpolateBiCubic(currentSegment, weightX, weightY);
                }
            }

            return octave;
        }

        private float[,] GenerateOctaveWithLinearInterpolation(uint octaveWaveLength, float octaveMaxValue)
        {
            var octave = new float[Width, Height];

            // Depending on the parameters, we need to add either 1 or 2 to cover the whole noise dimensions 
            // with the lattice. We always add 2 for simplicity, which covers all cases.
            uint latticeWidth = (Width / octaveWaveLength) + 2;
            uint latticeHeight = (Height / octaveWaveLength) + 2;
            float[,] lattice = GenerateRandomLattice(latticeWidth, latticeHeight, octaveMaxValue);

            for (uint x = 0; x < Width; x++)
            {
                for (uint y = 0; y < Height; y++)
                {
                    // The offset of the current lattice quad, i. e. the rectangle of values surrounded
                    // by 4 lattice points where this point is the one out of these 4 that is furthest
                    // to the bottom left.
                    uint latticeX = x / octaveWaveLength;
                    uint latticeY = y / octaveWaveLength;

                    // The 4 lattice points surrounding the segment of the noise from which the 
                    // intermediate noise values can be generated.
                    float bottomLeft    = lattice[latticeX    , latticeY    ];
                    float bottomRight   = lattice[latticeX + 1, latticeY    ];
                    float topLeft       = lattice[latticeX    , latticeY + 1];
                    float topRight      = lattice[latticeX + 1, latticeY + 1];

                    // The offset of the current coordinate within the containing lattice quad.
                    float weightX = (float)(x % latticeWidth) / (latticeWidth + 1);
                    float weightY = (float)(y % latticeHeight) / (latticeHeight + 1);

                    octave[x, y] = BiLerp(bottomLeft, bottomRight, topLeft, topRight, weightX, weightY);
                }
            }

            return octave;
        }

        /// <summary>
        /// Bi-cubic interpolation using 16 knots to compute a value lying between the
        /// knots, where the position relative to the knots is determined by
        /// the weight in each dimension.
        /// </summary>
        /// <param name="knots4x4">4 rows of points within the lattice, bottom to top.</param>
        /// <param name="weightX"></param>
        /// <param name="weightY"></param>
        /// <returns></returns>
        private float InterpolateBiCubic(float[][] knots4x4, float weightX, float weightY)
        {
            if (knots4x4.Length != 4 || knots4x4.Any(a => a.Length != 4))
            {
                throw new System.ArgumentException("Number of knots must be 4 in each dimension.");
            }

            float horiz0 = InterpolateCubic(knots4x4[0], weightX);
            float horiz1 = InterpolateCubic(knots4x4[1], weightX);
            float horiz2 = InterpolateCubic(knots4x4[2], weightX);
            float horiz3 = InterpolateCubic(knots4x4[3], weightX);

            var verticalPoints = new float[] { horiz0, horiz1, horiz2, horiz3 };

            return InterpolateCubic(verticalPoints, weightY);
        }

        /// <summary>
        /// Interpolate using Catmull-Rom cubic spline interpolation between 4 knots with a given weight.
        /// </summary>
        /// <param name="knots"></param>
        /// <param name="weight"></param>
        /// <returns>The interpolated value.</returns>
        private float InterpolateCubic(float[] knots, float weight)
        {
            if (knots.Length != 4)
            {
                throw new System.ArgumentException("Number of knots must be 4.");
            }

            float[,] coeff = new float[4, 4]
            {
                { 0.0f, 1.0f, 0.0f, 0.0f },
                { -0.5f, 0.0f, 0.5f, 0.0f },
                { 1.0f, -2.5f, 2.0f, -0.5f },
                { -0.5f, 1.5f, -1.5f, 0.5f }
            };

            float c0 = coeff[0, 0] * knots[0] + coeff[0, 1] * knots[1] + coeff[0, 2] * knots[2] + coeff[0, 3] * knots[3];
            float c1 = coeff[1, 0] * knots[0] + coeff[1, 1] * knots[1] + coeff[1, 2] * knots[2] + coeff[1, 3] * knots[3];
            float c2 = coeff[2, 0] * knots[0] + coeff[2, 1] * knots[1] + coeff[2, 2] * knots[2] + coeff[2, 3] * knots[3];
            float c3 = coeff[3, 0] * knots[0] + coeff[3, 1] * knots[1] + coeff[3, 2] * knots[2] + coeff[3, 3] * knots[3];

            return ((c3 * weight + c2) * weight + c1) * weight + c0;
        }

        /// <summary>
        /// Interpolate between 4 values a, b, c and d bilinearly, with weights for x and y dimension.
        /// </summary>
        private float BiLerp(float bottomLeft, float bottomRight, float topLeft, float topRight, float weightX, float weightY)
        {
            float interp1 = Lerp(bottomLeft, bottomRight, weightX);
            float interp2 = Lerp(topLeft, topRight, weightX);

            return Lerp(interp1, interp2, weightY);
        }

        /// <summary>
        /// Linearly interpolate between points a and b with the given weight.
        /// </summary>
        private float Lerp(float a, float b, float weight)
        {
            return a * (1 - weight) + b * weight;
        }

        /// <summary>
        /// Add all given matrices together.
        /// </summary>
        /// <param name="matrices"></param>
        /// <returns></returns>
        private float[,] SumMatrices(IEnumerable<float[,]> matrices)
        {
            var result = new float[Width, Height];

            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    result[Width, Height] = matrices
                        .Select(matrix => matrix[x, y])
                        .Aggregate((value1, value2) => value1 + value2);
                }
            }

            return result;
        }

        /// <summary>
        /// Create a new matrix by normalizing the values of a given matrix to the interval [0, 1].
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns>The normalized matrix.</returns>
        private float[,] NormalizeMatrix(float[,] matrix)
        {
            float[,] result = new float[matrix.GetLength(0), matrix.GetLength(1)];

            float minValue = 
                matrix.Cast<float>()
                .Aggregate((value1, value2) => Math.Min(value1, value2));

            float maxValue = 
                matrix.Cast<float>()
                .Aggregate((value1, value2) => Math.Max(value1, value2));

            // Calculate the normalized values.
            float range = Math.Abs(minValue - maxValue);

            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    float current = matrix[x, y] - minValue;
                    float normalized = current / range;
                    result[x, y] = normalized;
                }
            }

            return result;
        }
    }
}
