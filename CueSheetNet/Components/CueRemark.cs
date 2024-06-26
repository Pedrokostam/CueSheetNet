﻿using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet
{
    /// <summary>
    /// Represents an optional remark found in a CUE sheet.
    /// </summary>
    public sealed record CueRemark : IEquatable<CueRemark>
    {
        private string field;

        /// <summary>
        /// Name of the remark's field.
        /// </summary>
        public string Field
        {
            get => field;
            [MemberNotNull(nameof(field))]
            set => field = value.ToUpperInvariant();
        }

        /// <summary>
        /// The value of the remark.
        /// </summary>
        public string Value { get; set; }

        public CueRemark(string field, string value) => (Field, Value) = (field, value);

        /// <summary>
        /// Compares whether the current remark is the same as the other.
        /// They are considered equal, if their values and fields have the same value.
        /// <para/>
        /// Values are compared using the given StringComparison. Fields are comparaed ordinarily.
        /// </summary>
        public bool Equals(CueRemark? other, StringComparison valueComparisonType)
        {
            if (ReferenceEquals(other, this))
                return true;
            if (other == null)
                return false;
            if (!string.Equals(Field, other.Field, StringComparison.Ordinal))
                return false; // Fields are always uppercase
            return Value.Equals(other.Value, valueComparisonType);
        }

        /// <inheritdoc cref="Equals(CueRemark?, StringComparison)"/>
        /// <remarks>
        /// Uses CurrentCulture for value comparison.
        /// </remarks>
        public bool Equals(CueRemark? other)
        {
            return Equals(other, StringComparison.CurrentCulture);
        }

        public override int GetHashCode() => HashCode.Combine(Field, Value.ToUpperInvariant());

        public override string ToString()
        {
            return $"REM {Field} {Value}";
        }
    }
}
