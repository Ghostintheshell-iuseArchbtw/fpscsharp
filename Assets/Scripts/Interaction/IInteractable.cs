using UnityEngine;

/// <summary>
/// Interface for objects that can be interacted with by the player
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Get the prompt text to display when player can interact with this object
    /// </summary>
    /// <returns>The interaction prompt text</returns>
    string GetInteractionPrompt();
    
    /// <summary>
    /// Called when the player interacts with this object
    /// </summary>
    /// <param name="player">Reference to the player GameObject</param>
    void Interact(GameObject player);
}
