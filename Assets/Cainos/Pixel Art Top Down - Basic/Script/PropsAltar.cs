using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//when something get into the altar, make the runes glow
namespace Cainos.PixelArtTopDown_Basic
{
    public class PropsAltar : MonoBehaviour
    {
        public List<SpriteRenderer> runes;
        public float lerpSpeed = 3f;

        [Header("Detection")]
        [Tooltip("Use distance-based detection instead of trigger collider")]
        public bool useDistanceDetection = true;
        [Tooltip("Detection radius as multiplier of altar sprite size (1.0 = same as altar size)")]
        public float detectionRadiusMultiplier = 1.0f;
        [Tooltip("Runes start glowing from this distance (multiplier of detection radius)")]
        public float runeGlowStartMultiplier = 3.0f;  // Start glowing at 3x detection radius
        public string playerTag = "Player";
        public bool debugMode = false; // Enable debug to diagnose issues

        [Header("Interaction")]
        [Tooltip("Enable player interaction (tap to open unit creation menu)")]
        public bool enableInteraction = true;
        [Tooltip("Outline sprite renderer for highlight effect (optional)")]
        public SpriteRenderer outlineRenderer;
        [Tooltip("Highlight color for outline when player is near")]
        public Color highlightColor = new Color(0f, 0.95f, 1f, 0.8f); // Cyan matching rune color
        [Tooltip("Threshold for outline to appear (0-1, 0.8 = 80% rune brightness)")]
        public float outlineThreshold = 0.8f;

        private float actualDetectionRadius;
        private Vector3 detectionCenter; // Center point for detection (visual center of altar)

        private Color curColor;
        private Color targetColor;
        private Transform player;
        private bool isPlayerNear = false;
        private bool initialized = false;
        private bool canInteract = false;

        private void Awake()
        {
            // Check if runes are properly assigned
            bool hasValidRunes = runes != null && runes.Count > 0 && runes[0] != null;

            if (!hasValidRunes)
            {
                Debug.Log($"[PropsAltar] No runes assigned in inspector, auto-finding...");
                AutoFindRunes();
                hasValidRunes = runes != null && runes.Count > 0 && runes[0] != null;
            }

            // Calculate detection radius and center based on altar sprite size
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // Use the larger dimension of the sprite bounds, accounting for scale
                float spriteSize = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                actualDetectionRadius = spriteSize * detectionRadiusMultiplier * 0.5f; // 0.5 because bounds.size is full size, we want radius

                // Use sprite bounds center (visual center) instead of transform position (pivot)
                detectionCenter = sr.bounds.center;
            }
            else
            {
                actualDetectionRadius = 2f * detectionRadiusMultiplier; // Fallback
                detectionCenter = transform.position;
            }
            Debug.Log($"[PropsAltar] Detection radius: {actualDetectionRadius:F1}, center offset: {(detectionCenter - transform.position)}");

            if (hasValidRunes)
            {
                // Fix Z position and sorting order for all runes
                FixRunePositionsAndSorting();

                // Get the base color from first rune (cyan in original: r=0, g=0.95, b=1)
                Color runeColor = runes[0].color;
                // If rune color is black/invisible, use default cyan
                if (runeColor.r < 0.01f && runeColor.g < 0.01f && runeColor.b < 0.01f)
                {
                    Debug.Log($"[PropsAltar] Rune color is black, using default cyan");
                    runeColor = new Color(0f, 0.95f, 1f, 1f);
                }
                // targetColor keeps RGB, start with alpha=0 (invisible)
                targetColor = new Color(runeColor.r, runeColor.g, runeColor.b, 0f);
                curColor = targetColor;
                Debug.Log($"[PropsAltar] Initialized with {runes.Count} runes. RuneColor: {runeColor}, TargetColor: {targetColor}");
            }
            else
            {
                Debug.LogWarning($"[PropsAltar] No runes found!");
                // Set default cyan color (matching original rune color)
                targetColor = new Color(0f, 0.95f, 1f, 1f);
                curColor = targetColor;
                curColor.a = 0f;
            }
        }

        private void Start()
        {
            // Find player
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"[PropsAltar] Found player: {player.name}");
            }
            else
            {
                Debug.LogWarning($"[PropsAltar] Player with tag '{playerTag}' not found!");
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!useDistanceDetection && other.CompareTag(playerTag))
            {
                targetColor.a = 1.0f;
                isPlayerNear = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!useDistanceDetection && other.CompareTag(playerTag))
            {
                targetColor.a = 0.0f;
                isPlayerNear = false;
            }
        }

        private void Update()
        {
            // Distance-based detection (more reliable with scaled objects)
            if (useDistanceDetection)
            {
                if (player == null)
                {
                    GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
                    if (playerObj != null)
                    {
                        player = playerObj.transform;
                    }
                }

                if (player != null)
                {
                    // Use visual center of altar for distance calculation
                    float distance = Vector2.Distance(detectionCenter, player.position);
                    isPlayerNear = distance <= actualDetectionRadius;

                    // Calculate glow intensity based on distance
                    // Runes start glowing faintly from far away (runeGlowStartMultiplier * radius)
                    // and reach full brightness when player is at detection radius
                    float glowStartDistance = actualDetectionRadius * runeGlowStartMultiplier;

                    if (distance <= glowStartDistance)
                    {
                        // Map distance to alpha: far = 0, close = 1
                        // At glowStartDistance: alpha = 0
                        // At actualDetectionRadius: alpha = 1
                        float t = 1f - Mathf.Clamp01((distance - actualDetectionRadius) / (glowStartDistance - actualDetectionRadius));
                        targetColor.a = t;
                    }
                    else
                    {
                        targetColor.a = 0f;
                    }

                    if (debugMode && Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[PropsAltar] Distance: {distance:F1}, GlowStart: {glowStartDistance:F1}, Radius: {actualDetectionRadius:F1}, Alpha: {targetColor.a:F2}");
                    }

                    initialized = true;
                }
            }

            // Lerp rune colors
            curColor = Color.Lerp(curColor, targetColor, lerpSpeed * Time.deltaTime);

            if (runes == null || runes.Count == 0)
            {
                // Try to auto-find rune children
                AutoFindRunes();
            }

            foreach (var r in runes)
            {
                if (r != null)
                {
                    r.color = curColor;
                }
            }

            // Update interaction state
            canInteract = isPlayerNear && enableInteraction;

            // Update outline highlight - only show when runes are bright enough
            if (outlineRenderer != null)
            {
                if (curColor.a >= outlineThreshold)
                {
                    outlineRenderer.enabled = true;
                    // Outline color intensity matches rune intensity
                    Color outColor = highlightColor;
                    outColor.a = highlightColor.a * Mathf.Clamp01((curColor.a - outlineThreshold) / (1f - outlineThreshold));
                    outlineRenderer.color = outColor;
                }
                else
                {
                    outlineRenderer.enabled = false;
                }
            }
        }

        /// <summary>
        /// Check if player can interact with the altar.
        /// </summary>
        public bool CanInteract => canInteract;

        /// <summary>
        /// Check if player is near the altar.
        /// </summary>
        public bool IsPlayerNear => isPlayerNear;

        /// <summary>
        /// Called when player interacts with the altar (tap/click).
        /// Opens the unit creation menu.
        /// </summary>
        public void OnInteract()
        {
            if (!canInteract)
            {
                Debug.Log("[PropsAltar] Cannot interact - player not near altar");
                return;
            }

            Debug.Log("[PropsAltar] Player interacted with altar - opening unit creation menu");

            // Notify EventManager to open altar UI
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnOpenModal("altar", this);
            }
            else
            {
                Debug.LogWarning("[PropsAltar] EventManager not found - cannot open altar menu");
            }
        }

        /// <summary>
        /// Auto-find rune SpriteRenderers in children.
        /// In the original prefab, runes are under a "Rune" parent object and named "1", "2", "3", "4".
        /// </summary>
        private void AutoFindRunes()
        {
            if (runes == null)
                runes = new List<SpriteRenderer>();

            // Find ALL child SpriteRenderers (including nested ones)
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);

            foreach (var sr in childRenderers)
            {
                // Skip if it's the main altar sprite (the root object)
                if (sr.gameObject == gameObject) continue;

                // Skip shadow sprites
                string objName = sr.gameObject.name.ToLower();
                if (objName.Contains("shadow")) continue;

                // Check parent name - if parent is "Rune", this is a rune sprite
                Transform parent = sr.transform.parent;
                bool isUnderRuneParent = parent != null && parent.name.ToLower().Contains("rune");

                // Add if: under "Rune" parent, OR name contains rune/glow/light, OR is a numbered child (1,2,3,4)
                bool isNumberedChild = objName.Length <= 2 && char.IsDigit(objName[0]);

                if (isUnderRuneParent || objName.Contains("rune") || objName.Contains("glow") ||
                    objName.Contains("light") || objName.Contains("symbol") || isNumberedChild)
                {
                    if (!runes.Contains(sr))
                    {
                        runes.Add(sr);
                        if (debugMode) Debug.Log($"[PropsAltar] Found rune: {sr.gameObject.name} (parent: {parent?.name})");
                    }
                }
            }

            // If still no runes found, add ALL child SpriteRenderers except shadow and main
            if (runes.Count == 0)
            {
                foreach (var sr in childRenderers)
                {
                    if (sr.gameObject != gameObject && !sr.gameObject.name.ToLower().Contains("shadow") && !runes.Contains(sr))
                    {
                        runes.Add(sr);
                    }
                }
            }

            if (runes.Count > 0)
            {
                Debug.Log($"[PropsAltar] Auto-found {runes.Count} runes in children");

                // Fix Z position and ensure proper sorting
                // Runes in prefab have z=-2.5 which can cause issues with scaled objects
                SpriteRenderer altarRenderer = GetComponent<SpriteRenderer>();
                int altarSortingOrder = altarRenderer != null ? altarRenderer.sortingOrder : 0;

                foreach (var rune in runes)
                {
                    if (rune != null)
                    {
                        // Fix Z position
                        Vector3 pos = rune.transform.localPosition;
                        if (Mathf.Abs(pos.z) > 0.1f)
                        {
                            Debug.Log($"[PropsAltar] Fixing rune '{rune.name}' Z from {pos.z} to 0");
                            rune.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                        }

                        // Ensure runes render above altar
                        if (rune.sortingOrder <= altarSortingOrder)
                        {
                            Debug.Log($"[PropsAltar] Fixing rune '{rune.name}' sortingOrder from {rune.sortingOrder} to {altarSortingOrder + 1}");
                            rune.sortingOrder = altarSortingOrder + 1;
                        }
                    }
                }

                // Initialize color - preserve RGB from rune, start with alpha 0
                Color runeColor = runes[0].color;
                targetColor = new Color(runeColor.r, runeColor.g, runeColor.b, 0f);
                curColor = targetColor;
                Debug.Log($"[PropsAltar] Rune base color: {runeColor}, TargetColor: {targetColor}");
            }
            else
            {
                Debug.LogWarning("[PropsAltar] No runes found in children!");
            }
        }

        /// <summary>
        /// Fix Z position and sorting order for runes that are already assigned.
        /// </summary>
        private void FixRunePositionsAndSorting()
        {
            if (runes == null || runes.Count == 0) return;

            SpriteRenderer altarRenderer = GetComponent<SpriteRenderer>();
            int altarSortingOrder = altarRenderer != null ? altarRenderer.sortingOrder : 0;

            foreach (var rune in runes)
            {
                if (rune == null) continue;

                // Fix Z position - runes in prefab have z=-2.5 which causes issues with scaled objects
                Vector3 pos = rune.transform.localPosition;
                if (Mathf.Abs(pos.z) > 0.1f)
                {
                    Debug.Log($"[PropsAltar] Fixing rune '{rune.name}' Z from {pos.z} to 0");
                    rune.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
                }

                // Ensure runes render above altar
                if (rune.sortingOrder <= altarSortingOrder)
                {
                    Debug.Log($"[PropsAltar] Fixing rune '{rune.name}' sortingOrder from {rune.sortingOrder} to {altarSortingOrder + 1}");
                    rune.sortingOrder = altarSortingOrder + 1;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (useDistanceDetection)
            {
                Gizmos.color = isPlayerNear ? Color.cyan : Color.yellow;

                // Calculate center and radius
                Vector3 center;
                float radius;

                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    center = sr.bounds.center;
                    float spriteSize = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                    radius = spriteSize * detectionRadiusMultiplier * 0.5f;
                }
                else
                {
                    center = transform.position;
                    radius = 2f * detectionRadiusMultiplier;
                }

                Gizmos.DrawWireSphere(center, radius);
            }
        }
    }
}
