#pragma warning disable MA0008 // Add StructLayoutAttribute

namespace CueSheetNet.Components;

internal class Remcom : IRemarkable
{
    readonly List<CueRemark> _remarks = [];
    readonly List<string> _comments = [];

    public void AddRemark(string type, string value)
    {
        if (type.Equals("COMMENT", StringComparison.OrdinalIgnoreCase))
        {
            AddComment(value);
            return;
        }
        RemoveRemark(type);
        _remarks.Add(new CueRemark(type, value));
    }
    public void RemoveRemark(string type)
    {
        var old = _remarks.Find(x => x.Field.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (old is not null)
        {
            _remarks.Remove(old);
        }
    }
    public void RemoveRemark(CueRemark remark) => _remarks.Remove(remark);
    public void ClearRemarks() => _remarks.Clear();
    public IReadOnlyList<CueRemark> Remarks => _remarks.AsReadOnly();

    public void AddComment(string comment) => _comments.Add(comment);
    public void RemoveComment(string comment) => _comments.Remove(comment);
    public void ClearComments() => _comments.Clear();
    public IReadOnlyList<string> Comments => _comments.AsReadOnly();

    public string? GetRemarkValue(string field) {
        return _remarks.First(x => x.Field.Equals(field, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}

#pragma warning restore MA0008 // Add StructLayoutAttribute