using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet
{
    public sealed record CueRemark : IEquatable<CueRemark>
    {
        private string field;

        public string Field
        {
            get => field;
            [MemberNotNull(nameof(field))]
            set => field = value.ToUpperInvariant();
        }
        public string Value { get; set; }
        public CueRemark(string field, string value)
            => (Field, Value) = (field, value);
        public bool Equals(CueRemark? other, StringComparison valueComparisonType)
        {
            if (ReferenceEquals(other, this)) return true;
            if (other == null) return false;
            if (Field != other.Field) return false;
            return Value.Equals(other.Value, valueComparisonType);
        }
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