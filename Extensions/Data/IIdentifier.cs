namespace Extensions.Data;

public interface IIdentifier<out T> where T : IIdentifier<T>
{
    public T FromString(string str);
    public string StringRepresentation();
}