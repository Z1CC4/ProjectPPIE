using UnityEngine;

/// <summary>
/// Implement this on any object that the player can interact with (open/close, toggle, etc.).
/// The interactor will call Interact() when the player presses the interaction button.
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Optional text that can be shown in UI (e.g., "Apri frigo").
    /// Return null/empty if you don't want to show anything.
    /// </summary>
    string GetInteractionPrompt();

    /// <summary>
    /// Return false to temporarily block interaction (e.g., while animating).
    /// </summary>
    bool CanInteract();

    /// <summary>
    /// Perform the interaction (toggle open/close, etc.).
    /// </summary>
    void Interact(GameObject interactor);
}

