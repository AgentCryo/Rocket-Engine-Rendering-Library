using RCS;
using RCS.Components;

namespace Dev;

public class CubeComponent : IComponent
{
    public Entity Owner { get; set; }

    float _time = 0;
    
    public void Load() {}
    public void Update(float deltaTime)
    {
        Owner.Transform.Position.X = float.Sin(_time += deltaTime) * 3;
    }
    public void OnAdd() {}
}