using RCS.Components;
using System.Text;
using static RCS.Logger;

namespace RCS;

/// <summary>
/// Core static class for managing scenes, entities, and the active scene.
/// </summary>
public static class RCS_Core
{
    /// <summary>
    /// Represents a scene containing entities and providing update/load behavior.
    /// </summary>
    /// <param name="name">The name of the scene.</param>
    public class Scene(string name)
    {
        readonly string _name = name;
        /// <summary>
        /// Gets the name of the scene.
        /// </summary>
        public string GetName() => _name;
        readonly Dictionary<string, Entity> _entities = [];

        #region Entity Management

        /// <summary>
        /// Adds an entity to the scene.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        /// <returns>The current <see cref="Scene"/> instance for chaining.</returns>
        /// <exception cref="Exception">Thrown if an entity with the same name already exists.</exception>
        public Scene AddEntity(Entity entity)
        {
            if (_entities.ContainsKey(entity.GetName()))
                Error($"Entity with name {entity.GetName()} already exists"); //throw new Exception($"ERR: Entity with name {entity.GetName()} already exists");
            
            _entities.Add(entity.GetName(), entity);
            return this;
        }
        
        /// <summary>
        /// Removes an entity from the scene.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        /// <exception cref="Exception">Thrown if the entity does not exist.</exception>
        public void RemoveEntity(Entity entity)
        {
            if (!_entities.Remove(entity.GetName())) 
                Error($"Entity with name {entity.GetName()} was not found"); //throw new Exception($"ERR: Entity with name {entity.GetName()} was not found");
        }
        
        /// <summary>
        /// Removes an entity from the scene by name.
        /// </summary>
        /// <param name="name">The name of the entity to remove.</param>
        /// <exception cref="Exception">Thrown if the entity does not exist.</exception>
        public void RemoveEntity(string name)
        {
            if (!_entities.Remove(name, out var entity))
                Error($"Entity with name {name} was not found"); //throw new Exception($"ERR: Entity with name {name} was not found");
        }
        
        /// <summary>
        /// Removes all entities from the scene.
        /// </summary>
        public void RemoveAllEntities() => _entities.Clear();

        /// <summary>
        /// Attempts to retrieve an entity by name.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <param name="entity">The retrieved entity.</param>
        /// <exception cref="Exception">Thrown if the entity does not exist.</exception>
        public void GetEntity(string name, out Entity entity)
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            if (!_entities.TryGetValue(name, out entity!)) Error($"Entity with name {name} was not found"); //throw new Exception($"ERR: Entity with name {name} was not found");
        }
        
        /// <summary>
        /// Retrieves an entity by name.
        /// </summary>
        /// <param name="name">The name of the entity.</param>
        /// <returns>The entity with the given name.</returns>
        /// <exception cref="Exception">Thrown if the entity does not exist.</exception>
        public Entity GetEntity(string name)
        {
            if(_entities.TryGetValue(name, out var entity)) return entity;
            Error($"Entity with name {name} was not found"); //throw new Exception($"ERR: Entity with name {name} was not found");
            // ReSharper disable once NullableWarningSuppressionIsUsed
            return null!; //Should never be called as Error by default throws an Exception.
        }

        /// <summary>
        /// Retrieves a component of type <typeparamref name="T"/> from an entity.
        /// </summary>
        /// <typeparam name="T">The component type.</typeparam>
        /// <param name="name">The name of the entity.</param>
        /// <returns>The component instance.</returns>
        public T GetComponentFromEntity<T>(string name) where T : class, IComponent
        {
            return GetEntity(name).GetComponent<T>();
        }
        
        #endregion
        
        /// <summary>
        /// Calls <c>Load()</c> on all entities in the scene.
        /// </summary>
        public void Load()
        {
            foreach (var entity in _entities)
                entity.Value.Load();
        }

        /// <summary>
        /// Updates all entities in the scene.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last update.</param>
        public void Update(double deltaTime)
        {
            foreach (var entity in _entities)
                entity.Value.Update((float)deltaTime);
        }
    }

    static readonly Dictionary<string, Scene> Scenes = [];
    static string _activeScene = "";
    
    public static Scene GetActiveScene()
    {
        if (string.IsNullOrEmpty(_activeScene))
            Error($"Failed to get active scene: Active scene not set."); //throw new Exception($"ERR: Failed to get active scene: Active scene not set.");
        
        return Scenes[_activeScene];
    }

    /// <summary>
    /// Sets the active scene by name.
    /// </summary>
    /// <param name="name">The name of the scene to activate.</param>
    /// <exception cref="Exception">Thrown if the scene does not exist.</exception>
    public static void SetActiveScene(string name)
    {
        if (!Scenes.TryGetValue(name, out var scene))
            Error($"Scene with name {name} was not found"); //throw new Exception($"ERR: Scene with name {name} was not found");
        
        _activeScene = name;
    }

    /// <summary>
    /// Adds a scene to the engine.
    /// </summary>
    /// <param name="scene">The scene to add.</param>
    /// <exception cref="Exception">Thrown if a scene with the same name already exists.</exception>
    public static void AddScene(Scene scene)
    {
        if (Scenes.ContainsKey(scene.GetName()))
            Error($"Scene with name {scene.GetName()} already exists"); //throw new Exception($"ERR: Scene with name {scene.GetName()} already exists");

        Scenes.Add(scene.GetName(), scene);
    }

    /// <summary>
    /// Removes a scene from the engine.
    /// </summary>
    /// <param name="scene">The scene to remove.</param>
    /// <exception cref="Exception">Thrown if the scene does not exist.</exception>
    public static void RemoveScene(Scene scene)
    {
        if (!Scenes.Remove(scene.GetName()))
            Error($"Scene with name {scene.GetName()} was not found"); //throw new Exception($"ERR: Scene with name {scene.GetName()} was not found");
    }

    /// <summary>
    /// Removes a scene from the engine by name.
    /// </summary>
    /// <param name="name">The name of the scene to remove.</param>
    /// <exception cref="Exception">Thrown if the scene does not exist.</exception>
    public static void RemoveScene(string name)
    {
        if (!Scenes.Remove(name, out var scene))
            Error($"Scene with name {name} was not found."); //throw new Exception($"ERR: Scene with name {name} was not found");
    }

    /// <summary>
    /// Loads the currently active scene.
    /// </summary>
    /// <exception cref="Exception">Thrown if no active scene is set.</exception>
    public static void LoadActiveScene()
    {
        if (string.IsNullOrEmpty(_activeScene))
            Error("No active scene has been set."); //throw new Exception("ERR: No active scene has been set");

        Scenes[_activeScene].Load();
    }

    /// <summary>
    /// Updates the currently active scene.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <exception cref="Exception">Thrown if no active scene is set.</exception>
    public static void UpdateActiveScene(double deltaTime)
    {
        if (_activeScene is null)
            Error($"No active scene has been set."); //throw new Exception("ERR: No active scene has been set");

        Scenes[_activeScene].Update(deltaTime);
    }
}