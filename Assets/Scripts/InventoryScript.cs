using UnityEngine;
using System.Collections.Generic;

public class WeaponInventory : MonoBehaviour
{
	[Header("Weapon Settings")]
	public Transform weaponHolder; // Punto donde se colocan las armas (ej: mano del player)
	public int maxWeapons = 3; // Máximo número de armas que puede llevar

	[Header("Input Settings")]
	public KeyCode reloadKey = KeyCode.R;
	public KeyCode nextWeaponKey = KeyCode.E;
	public KeyCode prevWeaponKey = KeyCode.Q;
	public KeyCode pickupKey = KeyCode.F;

	[Header("Animation")]
	[SerializeField] private Animator playerAnimator; // Referencia al Animator del jugador

	private List<WeaponBase> weapons = new List<WeaponBase>(); // Armas recogidas
	private int currentWeaponIndex = -1;
	private WeaponBase currentWeapon;
	private WeaponPickup nearbyWeapon; // Arma cercana que se puede recoger

	// Switch cooldown to prevent spam
	private float lastSwitchTime = -1f;
	private const float SWITCH_COOLDOWN = 0.15f;

	// Track scroll state to only trigger once per scroll gesture
	private bool wasScrolling = false;

	// Animation parameter hash
	private static readonly int ArmaHash = Animator.StringToHash("arma");

	private void Start()
	{
		// Obtener el Animator si no está asignado
		if (playerAnimator == null)
		{
			playerAnimator = GetComponent<Animator>();
		}

		// Establecer arma = 0 (sin arma) al inicio
		if (playerAnimator != null)
		{
			playerAnimator.SetInteger(ArmaHash, 0);
		}
	}

	private void Update()
	{
		// No permitir acciones si el juego está en pausa
		if (PauseMenu.Instance != null && PauseMenu.Instance.IsPaused())
			return;

		HandleInput();
		CheckForWeaponPickup();
	}

	// Detectar armas cercanas para recoger
	private void CheckForWeaponPickup()
	{
		if (Input.GetKeyDown(pickupKey) && nearbyWeapon != null)
		{
			PickupWeapon(nearbyWeapon);
		}
	}

	// Detectar cuando el player entra en rango de un arma
	private void OnTriggerEnter(Collider other)
	{
		WeaponPickup pickup = other.GetComponent<WeaponPickup>();
		if (pickup != null)
		{
			nearbyWeapon = pickup;
			Debug.Log($"Press {pickupKey} to pick up {pickup.weaponPrefab.name}");
		}
	}

	// Detectar cuando el player sale del rango
	private void OnTriggerExit(Collider other)
	{
		WeaponPickup pickup = other.GetComponent<WeaponPickup>();
		if (pickup != null && pickup == nearbyWeapon)
		{
			nearbyWeapon = null;
		}
	}

	// Recoger un arma del suelo/pedestal
	private void PickupWeapon(WeaponPickup pickup)
	{
		AddWeapon(pickup.weaponPrefab);
		Destroy(pickup.gameObject); // Destruir el objeto del mundo
		nearbyWeapon = null;
	}

	// MODIFICADO: Equipar un arma por índice
	public void EquipWeapon(int index)
	{
		if (index < 0 || index >= weapons.Count)
			return;

		// Skip if already equipped
		if (index == currentWeaponIndex)
			return;

		// Check cooldown to prevent spam switching
		if (Time.time - lastSwitchTime < SWITCH_COOLDOWN)
		{
			Debug.Log($"[WeaponSwitch] BLOCKED by cooldown: {currentWeaponIndex} -> {index}");
			return;
		}

		Debug.Log($"[WeaponSwitch] Switching: {currentWeaponIndex} -> {index} (Frame: {Time.frameCount})");
		lastSwitchTime = Time.time;

		// Update animator FIRST so character visual changes before weapon swap
		UpdateAnimatorWeaponState(index + 1); // +1 porque 0 = sin arma

		// Desequipar arma actual
		if (currentWeapon != null)
		{
			currentWeapon.OnUnequip();
		}

		// Equipar nueva arma
		currentWeaponIndex = index;
		currentWeapon = weapons[currentWeaponIndex];
		currentWeapon.OnEquip();

		// Actualizar HUD - seleccionar slot
		HUDManager.Instance?.SeleccionarSlot(currentWeaponIndex);
	}

	// NUEVO: Actualizar el estado del arma en el Animator
	private void UpdateAnimatorWeaponState(int weaponState)
	{
		if (playerAnimator != null)
		{
			playerAnimator.SetInteger(ArmaHash, weaponState);
			Debug.Log($"Animator arma parameter set to: {weaponState}");
		}
	}

	// Cambiar a siguiente arma (with wrap-around)
	public void NextWeapon()
	{
		if (weapons.Count <= 1)
			return;

		int nextIndex = (currentWeaponIndex + 1) % weapons.Count;
		Debug.Log($"[WeaponSwitch] NextWeapon called: current={currentWeaponIndex}, next={nextIndex}");
		EquipWeapon(nextIndex);
	}

	// Cambiar a arma anterior (with wrap-around)
	public void PreviousWeapon()
	{
		if (weapons.Count <= 1)
			return;

		int prevIndex = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;
		Debug.Log($"[WeaponSwitch] PreviousWeapon called: current={currentWeaponIndex}, prev={prevIndex}");
		EquipWeapon(prevIndex);
	}

	// Añadir un arma nueva al inventario (pickup)
	public void AddWeapon(GameObject weaponPrefab)
	{
		// Verificar si ya tiene el máximo de armas
		if (weapons.Count >= maxWeapons)
		{
			Debug.Log($"Inventory full! Max {maxWeapons} weapons.");
			return;
		}

		GameObject weaponObj = Instantiate(weaponPrefab, weaponHolder);
		WeaponBase weapon = weaponObj.GetComponent<WeaponBase>();

		if (weapon != null)
		{
			weapons.Add(weapon);
			weapon.OnUnequip();
			Debug.Log($"Added weapon: {weapon.weaponName}");

			// Actualizar HUD - añadir icono del arma
			UpdateWeaponSlotHUD(weapons.Count - 1, weapon);

			// Si es la primera arma, equiparla automáticamente
			if (weapons.Count == 1)
			{
				EquipWeapon(0);
			}
		}
	}

	// Actualizar el icono del arma en el HUD
	private void UpdateWeaponSlotHUD(int slotIndex, WeaponBase weapon)
	{
		if (HUDManager.Instance != null)
		{
			// Aquí necesitas un sprite del icono del arma
			// Por ahora usamos null, pero deberías añadir un campo "weaponIcon" en WeaponBase
			Sprite weaponIcon = GetWeaponIcon(weapon);
			HUDManager.Instance.ActualizarSlotInventario(slotIndex, weaponIcon);
		}
	}

	// Obtener el icono del arma (modificar según tu implementación)
	private Sprite GetWeaponIcon(WeaponBase weapon)
	{
		return weapon.weaponIcon; // Ahora retorna el sprite del arma
	}

	// Disparar con el arma actual
	public void Fire()
	{
		if (currentWeapon != null)
		{
			currentWeapon.Fire();
		}
	}

	// Recargar arma actual
	public void Reload()
	{
		if (currentWeapon != null)
		{
			currentWeapon.Reload();
		}
	}

	// Manejo de inputs
	private void HandleInput()
	{
		if (currentWeapon == null)
			return;

		// Disparar con click izquierdo
		if (Input.GetMouseButton(0))
		{
			Fire();
		}

		// Recargar
		if (Input.GetKeyDown(reloadKey))
		{
			Reload();
		}

		// Cambiar arma (only one input source per frame)
		bool weaponSwitched = false;

		if (Input.GetKeyDown(nextWeaponKey) && !weaponSwitched)
		{
			NextWeapon();
			weaponSwitched = true;
		}

		if (Input.GetKeyDown(prevWeaponKey) && !weaponSwitched)
		{
			PreviousWeapon();
			weaponSwitched = true;
		}

		// Cambiar arma con rueda del ratón (only trigger once per scroll gesture)
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		bool isScrolling = Mathf.Abs(scroll) > 0.05f;

		if (isScrolling && !wasScrolling && !weaponSwitched)
		{
			if (scroll > 0)
				PreviousWeapon();
			else
				NextWeapon();
			weaponSwitched = true;
		}
		wasScrolling = isScrolling;

		// Cambiar arma con teclas numéricas
		for (int i = 0; i < weapons.Count && i < 9; i++)
		{
			if (Input.GetKeyDown(KeyCode.Alpha1 + i))
			{
				EquipWeapon(i);
			}
		}
	}

	// Obtener arma actual
	public WeaponBase GetCurrentWeapon()
	{
		return currentWeapon;
	}

	// Obtener info de munición actual
	public string GetCurrentAmmoInfo()
	{
		if (currentWeapon != null)
		{
			return currentWeapon.GetAmmoInfo();
		}
		return "0/0";
	}

	// MODIFICADO: Limpiar todo el inventario
	public void ClearInventory()
	{
		// Destruir todas las armas
		foreach (WeaponBase weapon in weapons)
		{
			if (weapon != null)
			{
				Destroy(weapon.gameObject);
			}
		}

		weapons.Clear();
		currentWeapon = null;
		currentWeaponIndex = -1;

		// NUEVO: Volver a estado sin arma (arma = 0)
		UpdateAnimatorWeaponState(0);

		// Limpiar slots del HUD solo si existe
		if (HUDManager.Instance != null)
		{
			for (int i = 0; i < maxWeapons; i++)
			{
				HUDManager.Instance.ActualizarSlotInventario(i, null);
			}
		}

		Debug.Log("Weapon inventory cleared");
	}

	// NUEVO: Método público para desequipar todas las armas (volver a arma = 0)
	public void UnequipAllWeapons()
	{
		if (currentWeapon != null)
		{
			currentWeapon.OnUnequip();
		}

		currentWeapon = null;
		currentWeaponIndex = -1;

		// Volver a estado sin arma
		UpdateAnimatorWeaponState(0);
	}
}
