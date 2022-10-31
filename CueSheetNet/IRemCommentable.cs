using System.Diagnostics.CodeAnalysis;

namespace CueSheetNet
{
    public record RemEntry : IEquatable<RemEntry>
    {
        private string field;

        public string Field
        {
            get => field;
            [MemberNotNull(nameof(field))]
            set => field = value.ToUpperInvariant();
        }
        public string Value { get; set; }
        public RemEntry(string field, string value)
            => (Field, Value) = (field, value);
        public bool Equals(RemEntry other, StringComparison valueComparisonType)
        {
            if (Field != other.Field) return false;
            return Value.Equals(other.Value, valueComparisonType);
        }
        public override string ToString()
        {
            return "REM " + Field + "  "+ Value;
        }

    }
    public interface IRemCommentable
    {
        void ClearComments();
        void AddComment(string comment);
        void RemoveComment(int index);
        void RemoveComment(string comment, StringComparison comparisonType);
        void ClearRems();
        void AddRem(string type, string value);
        void AddRem(RemEntry entry);
        void RemoveRem(int index);
        void RemoveRem(string type, string value, StringComparison comparisonType);
        void RemoveRem(RemEntry entry, StringComparison comparisonType);
    }
}