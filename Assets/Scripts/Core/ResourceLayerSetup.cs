using UnityEngine;

/// <summary>
/// Sets up Physics2D layer collision matrix so resources only collide with other resources.
/// Attach to a GameObject in the scene or use RuntimeInitializeOnLoadMethod.
/// </summary>
public class ResourceLayerSetup : MonoBehaviour
{
    private const string RESOURCE_LAYER = "Resources";
    private const int FALLBACK_LAYER = 8;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SetupResourceLayerCollisions()
    {
        int resourceLayer = LayerMask.NameToLayer(RESOURCE_LAYER);
        if (resourceLayer == -1)
        {
            resourceLayer = FALLBACK_LAYER;
        }

        // Disable collision between Resources layer and all other layers except itself
        for (int i = 0; i < 32; i++)
        {
            if (i != resourceLayer)
            {
                // Resources should NOT collide with other layers (player, units, etc.)
                Physics2D.IgnoreLayerCollision(resourceLayer, i, true);
            }
        }

        // Enable collision between resources themselves
        Physics2D.IgnoreLayerCollision(resourceLayer, resourceLayer, false);

        Debug.Log($"[ResourceLayerSetup] Configured layer {resourceLayer} to only collide with itself");
    }
}
