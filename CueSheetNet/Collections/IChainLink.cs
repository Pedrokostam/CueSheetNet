namespace CueSheetNet.Collections;

internal interface IChainLink<T>
{
    public T? Previous { get; set; }
    public T? Next { get; set; }

    public void GetPromoted();
}
