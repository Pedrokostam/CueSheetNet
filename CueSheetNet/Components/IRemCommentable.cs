namespace CueSheetNet
{
    /// <summary>
    /// Provides methods to modify comments and remarks of the parent item.
    /// </summary>
    internal interface IRemCommentable
    {
        void ClearComments();
        void AddComment(string comment);
        void RemoveComment(int index);
        void RemoveComment(string comment, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase);

        void ClearRemarks();
        void AddRemark(string type, string value);
        void AddRemark(CueRemark entry);
        void RemoveRemark(int index);
        void RemoveRemark(string type, string value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase);
        void RemoveRemark(CueRemark entry, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase);
    }
}