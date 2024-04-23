namespace CueSheetNet;

public partial class CueReader2
{
    private interface IChainLink<T>
    {
        public T? Previous { get; set; }
        public T? Next { get; set; }
    }

}