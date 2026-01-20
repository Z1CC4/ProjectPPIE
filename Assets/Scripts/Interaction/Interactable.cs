using UnityEngine;

public interface IInteractable
{
    string GetInteractionPrompt();

    bool CanInteract();

    void Interact(GameObject interactor);
}

