using RCS.Components;
using System.Text;

namespace RCS;

public static class RCS_Core
{
    public class Scene(string name)
    {
        readonly string _name = name;
        public string GetName() => _name;
        readonly Dictionary<string, Entity> _entities = [];

        public Scene AddEntity(Entity entity)
        {
            if (_entities.ContainsKey(entity.GetName()))
                throw new Exception($"ERR: Entity with name {entity.GetName()} already exists");
            
            _entities.Add(entity.GetName(), entity);
            return this;
        }
        public void RemoveEntity(Entity entity)
        {
            if (!_entities.Remove(entity.GetName())) 
                throw new Exception($"ERR: Entity with name {entity.GetName()} was not found");
        }
        public void RemoveEntity(string name)
        {
            if (!_entities.Remove(name, out var entity)) 
                throw new Exception($"ERR: Entity with name {name} was not found");
        }
        public void RemoveAllEntities() => _entities.Clear();

        public void GetEntity(string name, out Entity entity)
        {
            if (!_entities.TryGetValue(name, out entity!)) throw new Exception($"ERR: Entity with name {name} was not found");
        }
        public Entity GetEntity(string name)
        {
            if(_entities.TryGetValue(name, out var entity)) return entity;
            throw new Exception($"ERR: Entity with name {name} was not found");
        }

        public T GetComponentFromEntity<T>(string name) where T : class, IComponent
        {
            return GetEntity(name).GetComponent<T>();
        }
        
        public void Load()
        {
            foreach (var entity in _entities)
                entity.Value.Load();
        }

        public void Update(double deltaTime)
        {
            foreach (var entity in _entities)
                entity.Value.Update((float)deltaTime);
        }
    }

    static readonly Dictionary<string, Scene> Scenes = [];
    static string _activeScene = "";
    public static Scene GetActiveScene() {
        if(_activeScene != "") return Scenes[_activeScene];
        throw new Exception($"ERR: Failed to get active scene: Active scene not set.");
    }
    public static void SetActiveScene(string name)
    {
        if (Scenes.TryGetValue(name, out var scene)) {
            _activeScene = name;
        } else throw new Exception($"ERR: Scene with name {name} was not found");
    }

    public static void AddScene(Scene scene)
    {
        if (Scenes.ContainsKey(scene.GetName()))
            throw new Exception($"ERR: Scene with name {scene.GetName()} already exists");

        Scenes.Add(scene.GetName(), scene);
    }

    public static void RemoveScene(Scene scene)
    {
        if (!Scenes.Remove(scene.GetName()))
            throw new Exception($"ERR: Scene with name {scene.GetName()} was not found");
    }

    public static void RemoveScene(string name)
    {
        if (!Scenes.Remove(name, out var scene))
            throw new Exception($"ERR: Scene with name {name} was not found");
    }

    
    public static void LoadActiveScene()
    {
        if (_activeScene is null)
            throw new Exception("ERR: No active scene has been set");

        Scenes[_activeScene].Load();
    }

    public static void UpdateActiveScene(double deltaTime)
    {
        if (_activeScene is null)
            throw new Exception("ERR: No active scene has been set");

        Scenes[_activeScene].Update(deltaTime);
    }
}