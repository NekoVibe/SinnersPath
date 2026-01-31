using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class HUDManager : MonoBehaviour 
{
    public static HUDManager Instance { get; private set; }

    [Header("Vida y Economía")]
    [SerializeField] private Image[] hearts;
    [SerializeField] private TextMeshProUGUI coinsText;

    [Header("Timer y Combo")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private GameObject comboParent;

    [Header("Inventario")]
    [SerializeField] private Image[] inventorySlots;
    
    [Header("Sprites")]
    [SerializeField] private Sprite emptySlotSprite;
    [SerializeField] private Sprite heartFullSprite;
    [SerializeField] private Sprite heartEmptySprite;
    [SerializeField] private Sprite coinSprite;

    [Header("Configuración Animación")]
    [SerializeField] private float escalaSeleccionado = 1.3f;
    [SerializeField] private float velocidadAnimacion = 8f;
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
            Vector3 escalaObjetivo = (i == indiceSeleccionado) ? Vector3.one * escalaSeleccionado : Vector3.one;
            inventorySlots[i].transform.localScale = Vector3.Lerp(inventorySlots[i].transform.localScale, escalaObjetivo, Time.deltaTime * velocidadAnimacion);
        }
    }

    public void ActualizarHearts(int vidaActual) 
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].sprite = (i < vidaActual) ? heartFullSprite : heartEmptySprite;
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
