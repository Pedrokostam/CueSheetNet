namespace CueSheetNet.Components;

public interface IRemarkable
{
    IReadOnlyList<string> Comments { get; }
    IReadOnlyList<CueRemark> Remarks { get; }

    void AddComment(string comment);
    void AddRemark(string type, string value);
    void ClearComments();
    void ClearRemarks();
    void RemoveComment(string comment);
    void RemoveRemark(CueRemark remark);
    void RemoveRemark(string type);
}