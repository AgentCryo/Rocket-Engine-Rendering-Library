namespace RCS.Components;

/// <summary>
/// Defines the base behavior for all components in the Rocket Component System (RCS).
/// Components are attached to <see cref="Entity"/> objects and participate in the
/// entity lifecycle through load, update, and initialization callbacks.
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Gets or sets the <see cref="Entity"/> that owns this component.
    /// This is automatically assigned when the component is added to an entity.
    /// </summary>
    Entity Owner { get; set; }

    /// <summary>
    /// Called when the scene is first loaded.
    /// Use this method to set up internal state, acquire references,
    /// or perform any logic that should run once before updates begin.
    /// </summary>
    void Load();

    /// <summary>
    /// Called once per frame by the owning entity.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the previous frame.</param>
    void Update(float deltaTime);

    /// <summary>
    /// Called immediately after the component is added to an entity.
    /// Use this for initialization that depends on the entity or other components.
    /// </summary>
    void OnAdd();
}