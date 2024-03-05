public interface ISingleton<T> where T : ISingleton<T>, new()
{
    public static readonly T Instance = new T();
}