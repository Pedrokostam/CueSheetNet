﻿namespace CueSheetNet
{
    internal interface IRemarkableCommentable
    {
        void ClearComments();
        void AddComment(string comment);
        void RemoveComment(int index);
        void RemoveComment(string comment, StringComparison comparisonType);

        void ClearRemarks();
        void AddRemark(string type, string value);
        void AddRemark(CueRemark entry);
        void RemoveRemark(int index);
        void RemoveRemark(string type, string value, StringComparison comparisonType);
        void RemoveRemark(CueRemark entry, StringComparison comparisonType);
    }
}