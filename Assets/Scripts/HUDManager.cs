using UnityEngine;
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

    private void Awake() 
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        AnimarSeleccionInventario();
        SuavizarGolpeCombo();
    }

    public void SeleccionarSlot(int indice)
    {
        if (indice >= 0 && indice < inventorySlots.Length)
            indiceSeleccionado = indice;
    }

    private void AnimarSeleccionInventario()
    {
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            Vector3 escalaObjetivo = (i == indiceSeleccionado) ? Vector3.one * EscalaSeleccionado : Vector3.one;
            inventorySlots[i].transform.localScale = Vector3.Lerp(inventorySlots[i].transform.localScale, escalaObjetivo, Time.deltaTime * VelocidadAnimacion);
        }
    }

    public void ActualizarHearts(int vidaActual)
    {
        Debug.Log($"HUDManager.ActualizarHearts llamado con vida: {vidaActual}");

        if (heartImage == null)
        {
            Debug.LogError("HUDManager: heartImage es NULL!");
            return;
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

    public void ActualizarMonedas(int cantidad) => coinsText.text = cantidad.ToString();
    public void ActualizarReloj(string tiempoFormateado) => timerText.text = tiempoFormateado;

    public void ActualizarCombo(int valor) 
    {
        if (valor < 1) {
            comboParent.SetActive(false);
        } else {
            comboParent.SetActive(true);
            comboText.text = "x" + valor.ToString();
            comboText.transform.localScale = Vector3.one * 1.5f; 
        }
    }

    private void SuavizarGolpeCombo()
    {
        if (comboText.transform.localScale.x > 1.0f)
            comboText.transform.localScale = Vector3.Lerp(comboText.transform.localScale, Vector3.one, Time.deltaTime * 10f);
    }

    public void ActualizarSlotInventario(int indice, Sprite iconoItem) 
    {
        if (indice >= 0 && indice < inventorySlots.Length)
            inventorySlots[indice].sprite = (iconoItem != null) ? iconoItem : emptySlotSprite;
    }
}
