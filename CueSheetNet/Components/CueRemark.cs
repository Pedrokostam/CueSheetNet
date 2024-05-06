using System.Diagnostics.CodeAnalysis;
using CueSheetNet.Extensions;

namespace CueSheetNet
{
    /// <summary>
    /// Represents an optional remark found in a CUE sheet.
    /// </summary>
    public readonly record struct CueRemark
    {
        private readonly string field;

        /// <summary>
        /// Name of the remark's field.
        /// </summary>
        public string Field
        {
            get => field;
            [MemberNotNull(nameof(field))]
            init => field = value?.ToUpperInvariant() ?? string.Empty; 
        }

        /// <summary>
        /// The value of the remark.
        /// </summary>
        public string Value { get; init; }

        public CueRemark(string field, string value) => (Field, Value) = (field, value);

        public bool Equals(CueRemark other, IEqualityComparer<string>? comparer)
        {
            if (!Field.OrdEquals(other.Field))
            {
                return false; // Fields are always uppercase
            }

            comparer ??= StringComparer.Ordinal;
            return comparer.Equals(Value, other.Value);
        }
        /// <summary>
        /// Compares whether the current remark is the same as the other.
        /// They are considered equal, if their values and fields have the same value.
        /// <para/>
        /// Values are compared using the given StringComparison. Fields are compared ordinarily.
        /// </summary>
        public bool Equals(CueRemark other, StringComparison valueComparisonType)
        {
            var comparer = StringHelper.GetComparer(valueComparisonType);

            return Equals(other, comparer);
        }

        /// <inheritdoc cref="Equals(CueRemark?, StringComparison)"/>
        /// <remarks>
        /// Uses CurrentCulture for value comparison.
        /// </remarks>
        public bool Equals(CueRemark other)
        {
            return Equals(other, StringComparison.Ordinal);
        }


        public override int GetHashCode()
        {
            // Hashcode combine uses parameters' gethashocd. For string it is case-sensitive.
            return HashCode.Combine(Field, Value);
        }

        public override string ToString()
        {
            return $"REM {Field} {Value}";
        }
    }
}
