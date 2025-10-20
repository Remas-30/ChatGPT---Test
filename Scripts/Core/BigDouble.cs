using System;
using System.Globalization;

namespace GalacticExpansion.Core
{
    /// <summary>
    /// Lightweight scientific notation number helper used for very large idle values.
    /// Represents numbers as mantissa * 10^exponent.
    /// </summary>
    [Serializable]
    public struct BigDouble : IComparable<BigDouble>, IFormattable
    {
        private const double Log10Epsilon = 1e-12;

        /// <summary>
        /// Gets a representation of zero.
        /// </summary>
        public static readonly BigDouble Zero = new(0d, 0);

        /// <summary>
        /// Gets a representation of one.
        /// </summary>
        public static readonly BigDouble One = new(1d, 0);

        /// <summary>
        /// The mantissa component (normalized between -10 and 10).
        /// </summary>
        public double Mantissa;

        /// <summary>
        /// The base-10 exponent component.
        /// </summary>
        public int Exponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="BigDouble"/> struct.
        /// </summary>
        public BigDouble(double mantissa, int exponent)
        {
            Mantissa = mantissa;
            Exponent = exponent;
            Normalize();
        }

        /// <summary>
        /// Initializes a new instance from a standard double value.
        /// </summary>
        public static BigDouble FromDouble(double value)
        {
            if (Math.Abs(value) < double.Epsilon)
            {
                return Zero;
            }

            var exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            var mantissa = value / Math.Pow(10, exponent);
            return new BigDouble(mantissa, exponent);
        }

        /// <summary>
        /// Creates a new <see cref="BigDouble"/> from mantissa and exponent without normalisation.
        /// </summary>
        public static BigDouble FromUnnormalized(double mantissa, int exponent)
        {
            var value = new BigDouble
            {
                Mantissa = mantissa,
                Exponent = exponent
            };
            value.Normalize();
            return value;
        }

        /// <summary>
        /// Creates a new <see cref="BigDouble"/> from a base-10 exponent.
        /// </summary>
        public static BigDouble Pow10(double exponent)
        {
            if (double.IsNegativeInfinity(exponent))
            {
                return Zero;
            }

            int floor = (int)Math.Floor(exponent);
            double mantissa = Math.Pow(10d, exponent - floor);
            return new BigDouble(mantissa, floor);
        }

        /// <inheritdoc />
        public static implicit operator BigDouble(double value) => FromDouble(value);

        /// <inheritdoc />
        public static BigDouble operator +(BigDouble left, BigDouble right)
        {
            if (left.IsZero)
            {
                return right;
            }

            if (right.IsZero)
            {
                return left;
            }

            BigDouble max = left.Exponent >= right.Exponent ? left : right;
            BigDouble min = left.Exponent < right.Exponent ? left : right;
            int exponentDiff = max.Exponent - min.Exponent;

            if (exponentDiff > 12)
            {
                return max;
            }

            double mantissa = max.Mantissa + min.Mantissa * Math.Pow(10, -exponentDiff);
            return new BigDouble(mantissa, max.Exponent);
        }

        /// <inheritdoc />
        public static BigDouble operator -(BigDouble left, BigDouble right) => left + (-right);

        /// <inheritdoc />
        public static BigDouble operator -(BigDouble value) => new(-value.Mantissa, value.Exponent);

        /// <inheritdoc />
        public static BigDouble operator *(BigDouble left, BigDouble right)
        {
            if (left.IsZero || right.IsZero)
            {
                return Zero;
            }

            double mantissa = left.Mantissa * right.Mantissa;
            int exponent = left.Exponent + right.Exponent;
            return new BigDouble(mantissa, exponent);
        }

        /// <inheritdoc />
        public static BigDouble operator /(BigDouble left, BigDouble right)
        {
            if (right.IsZero)
            {
                throw new DivideByZeroException("Cannot divide BigDouble by zero");
            }

            if (left.IsZero)
            {
                return Zero;
            }

            double mantissa = left.Mantissa / right.Mantissa;
            int exponent = left.Exponent - right.Exponent;
            return new BigDouble(mantissa, exponent);
        }

        /// <inheritdoc />
        public static bool operator >(BigDouble left, BigDouble right) => left.CompareTo(right) > 0;

        /// <inheritdoc />
        public static bool operator <(BigDouble left, BigDouble right) => left.CompareTo(right) < 0;

        /// <inheritdoc />
        public static bool operator >=(BigDouble left, BigDouble right) => left.CompareTo(right) >= 0;

        /// <inheritdoc />
        public static bool operator <=(BigDouble left, BigDouble right) => left.CompareTo(right) <= 0;

        /// <summary>
        /// Gets a value indicating whether the value is effectively zero.
        /// </summary>
        public bool IsZero => Math.Abs(Mantissa) < Log10Epsilon;

        /// <summary>
        /// Converts the value to a <see cref="double"/>. Large values may overflow.
        /// </summary>
        public double ToDouble()
        {
            if (IsZero)
            {
                return 0d;
            }

            return Mantissa * Math.Pow(10d, Exponent);
        }

        /// <summary>
        /// Computes the base-10 logarithm.
        /// </summary>
        public double Log10()
        {
            if (IsZero)
            {
                return double.NegativeInfinity;
            }

            return Math.Log10(Math.Abs(Mantissa)) + Exponent;
        }

        /// <summary>
        /// Computes the natural logarithm.
        /// </summary>
        public double NaturalLog() => Log10() * Math.Log(10d);

        /// <summary>
        /// Raises the value to the given power.
        /// </summary>
        public BigDouble Pow(double power)
        {
            if (IsZero)
            {
                return Zero;
            }

            double resultLog10 = Log10() * power;
            BigDouble result = Pow10(resultLog10);
            if (Mantissa < 0d && Math.Abs(power % 2d) > Log10Epsilon)
            {
                result.Mantissa = -result.Mantissa;
            }

            return result;
        }

        /// <summary>
        /// Calculates a power using standard doubles and converts to <see cref="BigDouble"/>.
        /// </summary>
        public static BigDouble Pow(double value, double power)
        {
            if (Math.Abs(value) < double.Epsilon)
            {
                return Zero;
            }

            return FromDouble(Math.Pow(value, power));
        }

        /// <summary>
        /// Returns the maximum of two <see cref="BigDouble"/> values.
        /// </summary>
        public static BigDouble Max(BigDouble left, BigDouble right) => left >= right ? left : right;

        /// <summary>
        /// Clamps the current value so that it is at least <paramref name="min"/>.
        /// </summary>
        public BigDouble ClampMin(BigDouble min) => this < min ? min : this;

        /// <summary>
        /// Formats the value using a short human-readable representation.
        /// </summary>
        public string ToShortString(int significantDigits = 3, BigDoubleFormat format = BigDoubleFormat.Scientific)
        {
            if (IsZero)
            {
                return "0";
            }

            double log10 = Log10();
            int safeDigits = Math.Max(1, significantDigits);
            switch (format)
            {
                case BigDoubleFormat.Standard when Math.Abs(log10) < 6:
                    double rounded = Math.Round(ToDouble(), Math.Max(0, safeDigits));
                    return rounded.ToString($"N{Math.Max(0, safeDigits)}", CultureInfo.InvariantCulture);
                case BigDoubleFormat.Engineering:
                    int engExponent = (int)Math.Floor(log10 / 3d) * 3;
                    double engMantissa = Mantissa * Math.Pow(10d, Exponent - engExponent);
                    return string.Format(CultureInfo.InvariantCulture, $"{{0:F{safeDigits - 1}}}e{engExponent}", engMantissa);
                default:
                    return string.Format(CultureInfo.InvariantCulture, $"{{0:F{safeDigits - 1}}}e{Exponent}", Mantissa);
            }
        }

        /// <inheritdoc />
        public override string ToString() => ToString("G4", CultureInfo.InvariantCulture);

        /// <inheritdoc />
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (IsZero)
            {
                return 0d.ToString(format, formatProvider);
            }

            return string.Format(formatProvider, "{0}e{1}", Mantissa.ToString(format, formatProvider), Exponent);
        }

        /// <inheritdoc />
        public int CompareTo(BigDouble other)
        {
            if (IsZero && other.IsZero)
            {
                return 0;
            }

            if (Exponent == other.Exponent)
            {
                return Mantissa.CompareTo(other.Mantissa);
            }

            return Exponent.CompareTo(other.Exponent);
        }

        private void Normalize()
        {
            if (IsZero)
            {
                Mantissa = 0d;
                Exponent = 0;
                return;
            }

            double log10 = Math.Log10(Math.Abs(Mantissa));
            int floor = (int)Math.Floor(log10);
            Mantissa /= Math.Pow(10d, floor);
            Exponent += floor;

            if (Math.Abs(Mantissa) >= 10d)
            {
                Mantissa /= 10d;
                Exponent++;
            }
            else if (Math.Abs(Mantissa) < 1d)
            {
                Mantissa *= 10d;
                Exponent--;
            }
        }
    }

    /// <summary>
    /// Enumerates supported compact formatting modes for <see cref="BigDouble.ToShortString"/>.
    /// </summary>
    public enum BigDoubleFormat
    {
        /// <summary>
        /// Standard formatting (falls back to scientific for large magnitudes).
        /// </summary>
        Standard,

        /// <summary>
        /// Scientific formatting (mantissa e exponent).
        /// </summary>
        Scientific,

        /// <summary>
        /// Engineering formatting (exponent aligned to multiples of three).
        /// </summary>
        Engineering
    }
}
