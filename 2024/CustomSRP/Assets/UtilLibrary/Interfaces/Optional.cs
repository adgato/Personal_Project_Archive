public struct Optional<T> where T : class
{
    private readonly T value;
    public Optional(T optional)
    {
        value = optional;
    }
    /// <summary>
    /// Useful constructor if evaluating <paramref name="Optional"/> may throw an Exception if not <paramref name="predicate"/>
    /// </summary>
    public Optional(bool predicate, System.Func<T> Optional)
    {
        value = predicate ? Optional() : null;
    }

    /// <returns>False iff the <see cref="value"/> is null</returns>
    public bool TryGet(ref T value)
    {
        if (this.value == null)
            return false;
        value = this.value;
        return true;
    }
}
