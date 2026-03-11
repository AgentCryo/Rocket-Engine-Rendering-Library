using RCS.Components;

namespace RCS;

public class Entity(string name)
{
    string _name = name;
    List<IComponent> _components = new();
    
    public string GetName() => _name;

    public Entity AddComponent(IComponent component)
    {
        component.Owner = this;
        _components.Add(component);
        component.OnAdd();
        return this;
    }
    public Entity AddComponent<T>() where T : IComponent, new()
    {
        var component = new T{Owner = this};
        _components.Add(component);
        component.OnAdd();
        return this;
    }
    
    public T GetComponent<T>() where T : class, IComponent => _components.OfType<T>().FirstOrDefault() ?? throw new Exception($"ERR: Component of type: {typeof(T).Name} not found in: {_name}.");

    public bool TryGetComponent<T>(out T? component) where T : class, IComponent
    {
        component = _components.OfType<T>().FirstOrDefault();
        return component != null;
    }
    public void RemoveAllComponents() => _components.Clear();
    public void RemoveComponent(IComponent component) => _components.Remove(component);

    public virtual void Load()
    {
        foreach (var component in _components) {
            component.Load();
        }
    }
    
    public virtual void Update(float deltaTime)
    {
        foreach (var component in _components) {
            component.Update(deltaTime);
        }
    }
}