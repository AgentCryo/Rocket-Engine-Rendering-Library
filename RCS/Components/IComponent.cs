namespace RCS.Components;

public interface IComponent
{
    Entity Owner { get; set; }
    public void Load();
    public void Update(float deltaTime);
    void OnAdd();
}