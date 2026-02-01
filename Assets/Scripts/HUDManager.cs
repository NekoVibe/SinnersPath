using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("Vida y Economía")]
    [SerializeField] private Image heartImage;
    [SerializeField] private TextMeshProUGUI coinsText;

    [Header("Timer y Combo")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private GameObject comboParent;

    [Header("Inventario")]
    [SerializeField] private Image[] inventorySlots;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySlotSprite;
	[SerializeField] private Sprite spriteHeartOneLife;
	[SerializeField] private Sprite spriteHeartTwoLifes;
	[SerializeField] private Sprite spriteFullHeart;
	[SerializeField] private Sprite spriteEmptyHeart;
    [SerializeField] private Sprite coinSprite;

    [Header("Configuración Animación")]
    [SerializeField] private float EscalaSeleccionado = 1.3f;
    [SerializeField] private float VelocidadAnimacion = 8f;
    private int indiceSeleccionado = 0;

    // Flag to track if we need to find UI references
    private bool uiReferencesBound = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to scene loaded event to rebind UI references
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Try to bind UI references on start
        TryBindUIReferences();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset flag so we try to find UI again
        uiReferencesBound = false;
        // Delay binding to next frame to ensure UI is instantiated
        StartCoroutine(DelayedBindUI());
    }

    private System.Collections.IEnumerator DelayedBindUI()
    {
        yield return null; // Wait one frame
        yield return null; // Wait another frame for UI to fully initialize
        TryBindUIReferences();
    }

    /// <summary>
    /// Finds and binds UI references at runtime.
    /// This handles the case where HUDManager is in a different prefab than the UI.
    /// </summary>
    private void TryBindUIReferences()
    {
        if (uiReferencesBound && heartImage != null) return;

        Debug.Log("[HUDManager] Attempting to bind UI references...");

        // Find UI root (try multiple possible names, including "(Clone)" suffix from instantiation)
        GameObject uiRoot = GameObject.Find("UI(Clone)");
        if (uiRoot == null) uiRoot = GameObject.Find("UI");
        if (uiRoot == null) uiRoot = GameObject.Find("HUDCanvas");
        if (uiRoot == null) uiRoot = GameObject.Find("HUDCanvas(Clone)");

        if (uiRoot == null)
        {
            Debug.LogWarning("[HUDManager] Could not find UI root in scene!");
            return;
        }

        Debug.Log($"[HUDManager] Found UI root: {uiRoot.name}");

        // Find heart image (actual name: Heart1)
        if (heartImage == null)
        {
            Transform heartTransform = FindChildRecursive(uiRoot.transform, "Heart1");
            if (heartTransform == null) heartTransform = FindChildRecursive(uiRoot.transform, "HeartImage");
            if (heartTransform == null) heartTransform = FindChildRecursive(uiRoot.transform, "Heart");
            if (heartTransform != null)
            {
                heartImage = heartTransform.GetComponent<Image>();
                Debug.Log("[HUDManager] Found heartImage");
            }
        }

        // Find coins text (actual name: Coins_Text)
        if (coinsText == null)
        {
            Transform coinsTransform = FindChildRecursive(uiRoot.transform, "Coins_Text");
            if (coinsTransform == null) coinsTransform = FindChildRecursive(uiRoot.transform, "CoinsText");
            if (coinsTransform == null) coinsTransform = FindChildRecursive(uiRoot.transform, "Monedas");
            if (coinsTransform != null)
            {
                coinsText = coinsTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log("[HUDManager] Found coinsText");
            }
        }

        // Find timer text (actual name: Timer_Text)
        if (timerText == null)
        {
            Transform timerTransform = FindChildRecursive(uiRoot.transform, "Timer_Text");
            if (timerTransform == null) timerTransform = FindChildRecursive(uiRoot.transform, "TimerText");
            if (timerTransform == null) timerTransform = FindChildRecursive(uiRoot.transform, "Reloj");
            if (timerTransform != null)
            {
                timerText = timerTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log("[HUDManager] Found timerText");
            }
        }

        // Find combo text and parent (actual names: Combo_Text, Combo_Parent)
        if (comboText == null)
        {
            Transform comboTransform = FindChildRecursive(uiRoot.transform, "Combo_Text");
            if (comboTransform == null) comboTransform = FindChildRecursive(uiRoot.transform, "ComboText");
            if (comboTransform != null)
            {
                comboText = comboTransform.GetComponent<TextMeshProUGUI>();
                Debug.Log("[HUDManager] Found comboText");
            }

            // Find combo parent separately
            Transform comboParentTransform = FindChildRecursive(uiRoot.transform, "Combo_Parent");
            if (comboParentTransform != null)
            {
                comboParent = comboParentTransform.gameObject;
            }
            else if (comboText != null)
            {
                comboParent = comboText.transform.parent != null ? comboText.transform.parent.gameObject : comboText.gameObject;
            }
        }

        // Find inventory slots (actual names: Slot1, Slot2, Slot3 inside Inventory_Panel)
        if (inventorySlots == null || inventorySlots.Length == 0 || inventorySlots[0] == null)
        {
            Transform inventoryParent = FindChildRecursive(uiRoot.transform, "Inventory_Panel");
            if (inventoryParent == null) inventoryParent = FindChildRecursive(uiRoot.transform, "InventorySlots");

            if (inventoryParent != null)
            {
                // Find slots by name in order
                var slotsList = new System.Collections.Generic.List<Image>();
                for (int i = 1; i <= 3; i++)
                {
                    Transform slotTransform = FindChildRecursive(inventoryParent, $"Slot{i}");
                    if (slotTransform != null)
                    {
                        Image slotImage = slotTransform.GetComponent<Image>();
                        if (slotImage != null)
                            slotsList.Add(slotImage);
                    }
                }

                if (slotsList.Count > 0)
                {
                    inventorySlots = slotsList.ToArray();
                    Debug.Log($"[HUDManager] Found {inventorySlots.Length} inventory slots");
                }
            }
        }

        uiReferencesBound = (heartImage != null);

        if (uiReferencesBound)
        {
            Debug.Log("[HUDManager] UI references bound successfully!");
        }
        else
        {
            Debug.LogWarning("[HUDManager] Some UI references could not be found. Check UI element names.");
        }
    }

    /// <summary>
    /// Recursively finds a child transform by name (including inactive objects)
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    private void Update()
    {
        AnimarSeleccionInventario();
        SuavizarGolpeCombo();
    }

    public void SeleccionarSlot(int indice)
    {
        if (inventorySlots == null || inventorySlots.Length == 0)
        {
            TryBindUIReferences();
        }

        if (inventorySlots != null && indice >= 0 && indice < inventorySlots.Length)
            indiceSeleccionado = indice;
    }

    private void AnimarSeleccionInventario()
    {
        if (inventorySlots == null || inventorySlots.Length == 0) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            Vector3 escalaObjetivo = (i == indiceSeleccionado) ? Vector3.one * EscalaSeleccionado : Vector3.one;
            inventorySlots[i].transform.localScale = Vector3.Lerp(inventorySlots[i].transform.localScale, escalaObjetivo, Time.deltaTime * VelocidadAnimacion);
        }
    }

    public void ActualizarHearts(int vidaActual)
    {
        Debug.Log($"HUDManager.ActualizarHearts llamado con vida: {vidaActual}");

        if (heartImage == null)
        {
            TryBindUIReferences();
            if (heartImage == null)
            {
                Debug.LogWarning("[HUDManager] heartImage not found - UI may not be loaded yet");
                return;
            }
        }

        Sprite nuevoSprite = vidaActual switch
        {
            0 => spriteEmptyHeart,
            1 => spriteHeartOneLife,
            2 => spriteHeartTwoLifes,
            _ => spriteFullHeart // 3 o más
        };

        if (nuevoSprite == null)
        {
            Debug.LogError($"HUDManager: Sprite para vida {vidaActual} es NULL!");
            return;
        }

        heartImage.sprite = nuevoSprite;
        Debug.Log($"HUDManager: Sprite actualizado correctamente para vida {vidaActual}");
    }

    public void ActualizarMonedas(int cantidad)
    {
        if (coinsText == null)
        {
            TryBindUIReferences();
            if (coinsText == null) return;
        }
        coinsText.text = cantidad.ToString();
    }

    public void ActualizarReloj(string tiempoFormateado)
    {
        if (timerText == null)
        {
            TryBindUIReferences();
            if (timerText == null) return;
        }
        timerText.text = tiempoFormateado;
    }

    public void ActualizarCombo(int valor)
    {
        if (comboParent == null || comboText == null)
        {
            TryBindUIReferences();
            if (comboParent == null || comboText == null) return;
        }

        if (valor < 1)
        {
            comboParent.SetActive(false);
        }
        else
        {
            comboParent.SetActive(true);
            comboText.text = "x" + valor.ToString();
            comboText.transform.localScale = Vector3.one * 1.5f;
        }
    }

    private void SuavizarGolpeCombo()
    {
        if (comboText == null) return;

        if (comboText.transform.localScale.x > 1.0f)
            comboText.transform.localScale = Vector3.Lerp(comboText.transform.localScale, Vector3.one, Time.deltaTime * 10f);
    }

    public void ActualizarSlotInventario(int indice, Sprite iconoItem)
    {
        if (inventorySlots == null || inventorySlots.Length == 0)
        {
            TryBindUIReferences();
        }

        if (inventorySlots == null || indice < 0 || indice >= inventorySlots.Length)
            return;

        Image slot = inventorySlots[indice];
        if (slot == null)
            return;

        if (iconoItem != null)
        {
            // Hay arma: mostrar icono
            slot.sprite = iconoItem;
            slot.color = Color.white;
        }
        else if (emptySlotSprite != null)
        {
            // No hay arma pero hay sprite vacío: usarlo
            slot.sprite = emptySlotSprite;
            slot.color = Color.white;
        }
        else
        {
            // No hay arma ni sprite vacío: hacer transparente
            slot.sprite = null;
            slot.color = Color.clear;
        }
    }

    // Resetear todos los slots del inventario (llamar al reiniciar)
    public void ResetInventorySlots()
    {
        if (inventorySlots == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            ActualizarSlotInventario(i, null);
        }
        indiceSeleccionado = 0;
    }

    /// <summary>
    /// Force rebind UI references (useful for debug or after dynamic UI changes)
    /// </summary>
    public void ForceRebindUI()
    {
        uiReferencesBound = false;
        TryBindUIReferences();
    }
}
