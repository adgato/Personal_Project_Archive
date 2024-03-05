using System;

[Serializable]
public class UniqueID : IComparable<UniqueID>
{
    private static int counter;
    public readonly int ID;
    private UniqueID()
    {
        ID = counter++;
    }
    public static UniqueID GetNext() => new UniqueID();

    public int CompareTo(UniqueID other) => ID.CompareTo(other.ID);

    public static implicit operator int(UniqueID obj) => obj.ID;
}
