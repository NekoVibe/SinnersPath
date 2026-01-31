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

	private List<WeaponBase> weapons = new List<WeaponBase>(); // Armas recogidas
	private int currentWeaponIndex = -1;
	private WeaponBase currentWeapon;
	private WeaponPickup nearbyWeapon; // Arma cercana que se puede recoger

	private void Start()
	{
		// El inventario empieza vacío
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

	// Equipar un arma por índice
	public void EquipWeapon(int index)
	{
		if (index < 0 || index >= weapons.Count)
			return;

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

	// Cambiar a siguiente arma
	public void NextWeapon()
	{
		if (weapons.Count <= 1)
			return;

		int nextIndex = (currentWeaponIndex + 1) % weapons.Count;
		EquipWeapon(nextIndex);
	}

	// Cambiar a arma anterior
	public void PreviousWeapon()
	{
		if (weapons.Count <= 1)
			return;

		int prevIndex = currentWeaponIndex - 1;
		if (prevIndex < 0)
			prevIndex = weapons.Count - 1;

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

		// Cambiar arma
		if (Input.GetKeyDown(nextWeaponKey))
		{
			NextWeapon();
		}

		if (Input.GetKeyDown(prevWeaponKey))
		{
			PreviousWeapon();
		}

		// Cambiar arma con rueda del ratón
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		if (scroll > 0f)
		{
			NextWeapon();
		}
		else if (scroll < 0f)
		{
			PreviousWeapon();
		}

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

	// Limpiar todo el inventario (útil al reiniciar)
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
}
