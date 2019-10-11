namespace LogReceiver

{
    public interface IHasKey<TKey>
    {
        TKey Key { get; }
    }
}
