namespace WebFormsCore.Features;

public interface IClientDependencyCollection
{
    IReadOnlyList<IClientDependencyFile> Files { get; }

    void Add(IClientDependencyFile file);

    void Remove(IClientDependencyFile file);

    void Remove(ClientDependencyType dependencyType, string name);

    void RemoveAll(Predicate<IClientDependencyFile> predicate);
}

internal class ClientDependencyCollection : IClientDependencyCollection
{
    public List<IClientDependencyFile> Files { get; } = new();

    public void Add(IClientDependencyFile file)
    {
        if (file.Name is not null)
        {
            Remove(file.DependencyType, file.Name);
        }

        Files.Add(file);
    }

    public void Remove(IClientDependencyFile file)
    {
        Files.Remove(file);
    }

    public void Remove(ClientDependencyType dependencyType, string name)
    {
        var item = GetByName(dependencyType, name);

        if (item != null)
        {
            Files.Remove(item);
        }
    }

    public void RemoveAll(Predicate<IClientDependencyFile> predicate)
    {
        Files.RemoveAll(predicate);
    }

    private IClientDependencyFile? GetByName(ClientDependencyType dependencyType, string name)
    {
        return Files.FirstOrDefault(x => x.DependencyType == dependencyType &&
                                         string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    IReadOnlyList<IClientDependencyFile> IClientDependencyCollection.Files => Files;
}
