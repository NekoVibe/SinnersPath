using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
	[Header("Weapon Info")]
	public string weaponName = "Weapon";
	public Sprite weaponIcon;

	[Header("Animation")]
	protected Animator playerAnimator; // Se asigna automáticamente

	// Animation parameter hash
	protected static readonly int PunchHash = Animator.StringToHash("Punch");

	protected virtual void Start()
	{
		// Intentar buscar el animator del jugador
		if (playerAnimator == null)
		{
			// Opción 1: Buscar en el padre
			playerAnimator = GetComponentInParent<Animator>();

			// Opción 2: Buscar por tag "Player"
			if (playerAnimator == null)
			{
				GameObject player = GameObject.FindGameObjectWithTag("Player");
				if (player != null)
				{
					playerAnimator = player.GetComponent<Animator>();
				}
			}

			// Opción 3: Buscar en PlayerPersistence
			if (playerAnimator == null && PlayerPersistence.Instance != null)
			{
				playerAnimator = PlayerPersistence.Instance.GetComponent<Animator>();
			}

			if (playerAnimator == null)
			{
				Debug.LogError($"{weaponName}: CRITICAL - No Animator found! Attack animations won't work.");
			}
			else
			{
				Debug.Log($"{weaponName}: Animator found successfully");
			}
		}
	}

	// Método para asignar el animator externamente (desde WeaponInventory)
	public void SetPlayerAnimator(Animator animator)
	{
		playerAnimator = animator;
		Debug.Log($"{weaponName}: Animator manually assigned");
	}

	// Método abstracto para disparar
	public abstract void Fire();

	// Triggerar animación de ataque
	protected void TriggerAttackAnimation()
	{
		if (playerAnimator != null)
		{
			playerAnimator.SetTrigger(PunchHash);

			// Debug para verificar
			int armaValue = playerAnimator.GetInteger("arma");
			Debug.Log($"{weaponName}: Attack triggered! Current arma value: {armaValue}");
		}
		else
		{
			Debug.LogError($"{weaponName}: Cannot trigger attack - Animator is NULL!");
		}
	}

	// Método virtual para recargar
	public virtual void Reload() { }

	// Cuando se equipa el arma
	public virtual void OnEquip()
	{
		gameObject.SetActive(true);
	}

	// Cuando se desequipa el arma
	public virtual void OnUnequip()
	{
		gameObject.SetActive(false);
	}

	// Obtener info de munición
	public virtual string GetAmmoInfo()
	{
		return "∞/∞";
	}
}
