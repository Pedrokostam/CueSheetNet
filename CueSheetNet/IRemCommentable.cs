using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet
{
    public sealed record Remark : IEquatable<Remark>
    {
        private string field;

        public string Field
        {
            get => field;
            [MemberNotNull(nameof(field))]
            set => field = value.ToUpperInvariant();
        }
        public string Value { get; set; }
        public Remark(string field, string value)
            => (Field, Value) = (field, value);
        public bool Equals(Remark? other, StringComparison valueComparisonType)
        {
            if (ReferenceEquals(other, this)) return true;
            if (other == null) return false;
            if (Field != other.Field) return false;
            return Value.Equals(other.Value, valueComparisonType);
        }
        public bool Equals(Remark? other)
        {
            return Equals(other, StringComparison.CurrentCulture);
        }
        public override int GetHashCode() => HashCode.Combine(Field, Value.ToUpperInvariant());
        public override string ToString()
        {
            return $"REM {Field} {Value}";
        }

    }
    internal interface IRemarkableCommentable
    {
        void ClearComments();
        void AddComment(string comment);
        void RemoveComment(int index);
        void RemoveComment(string comment, StringComparison comparisonType);

        void ClearRemarks();
        void AddRemark(string type, string value);
        void AddRemark(Remark entry);
        void RemoveRemark(int index);
        void RemoveRemark(string type, string value, StringComparison comparisonType);
        void RemoveRemark(Remark entry, StringComparison comparisonType);
    }
}