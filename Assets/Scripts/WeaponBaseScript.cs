using UnityEngine;

// Enum para tipos de armas
public enum WeaponType
{
	Melee,   // Armas cuerpo a cuerpo (sin munición)
	Ranged   // Armas de rango (con munición)
}

public class WeaponBase : MonoBehaviour
{
	[Header("Weapon Properties")]
	public string weaponName = "Weapon";
	public Sprite weaponIcon; // Icono para mostrar en el HUD
	public WeaponType weaponType = WeaponType.Ranged; // Tipo de arma
	public float damage = 10f;
	public float fireRate = 0.5f; // Tiempo entre disparos/ataques

	[Header("Ammo (solo para armas de rango)")]
	public int maxAmmo = 30;
	public int currentAmmo;
	public float reloadTime = 2f;

	[Header("References")]
	public Transform firePoint; // Punto desde donde sale el proyectil
	public GameObject projectilePrefab; // Prefab del proyectil (opcional)

	protected bool isReloading = false;
	protected float nextFireTime = 0f;

	private void Start()
	{
		// Solo inicializar munición para armas de rango
		if (weaponType == WeaponType.Ranged)
		{
			currentAmmo = maxAmmo;
		}
		else
		{
			currentAmmo = -1; // -1 indica munición infinita (melee)
		}
	}

	// Método principal para disparar/atacar (override en armas específicas)
	public virtual void Fire()
	{
		// Verificar cooldown
		if (isReloading || Time.time < nextFireTime)
			return;

		// Para armas de rango, verificar munición
		if (weaponType == WeaponType.Ranged)
		{
			if (currentAmmo <= 0)
			{
				Debug.Log($"{weaponName} out of ammo!");
				return;
			}
			currentAmmo--;
		}

		nextFireTime = Time.time + fireRate;

		// Implementar disparo/ataque específico en clases hijas
		Debug.Log($"{weaponName} fired! Ammo: {GetAmmoInfo()}");
	}

	// Método para recargar (solo para armas de rango)
	public virtual void Reload()
	{
		// Las armas melee no se recargan
		if (weaponType == WeaponType.Melee)
		{
			Debug.Log($"{weaponName} is a melee weapon, no reload needed");
			return;
		}

		if (isReloading || currentAmmo == maxAmmo)
			return;

		StartCoroutine(ReloadCoroutine());
	}

	private System.Collections.IEnumerator ReloadCoroutine()
	{
		isReloading = true;
		Debug.Log($"Reloading {weaponName}...");

		yield return new WaitForSeconds(reloadTime);

		currentAmmo = maxAmmo;
		isReloading = false;
		Debug.Log($"{weaponName} reloaded!");
	}

	// Método llamado cuando se equipa el arma
	public virtual void OnEquip()
	{
		gameObject.SetActive(true);
		Debug.Log($"{weaponName} equipped");
	}

	// Método llamado cuando se desequipa el arma
	public virtual void OnUnequip()
	{
		gameObject.SetActive(false);
	}

	// Para mostrar info del arma
	public string GetAmmoInfo()
	{
		if (weaponType == WeaponType.Melee)
		{
			return "∞"; // Símbolo de infinito para melee
		}
		return $"{currentAmmo}/{maxAmmo}";
	}

	// Verificar si el arma necesita munición
	public bool UsesAmmo()
	{
		return weaponType == WeaponType.Ranged;
	}

	// Verificar si el arma puede disparar/atacar
	public bool CanFire()
	{
		// Verificar cooldown
		if (isReloading || Time.time < nextFireTime)
			return false;

		// Las armas melee siempre pueden atacar (sin munición)
		if (weaponType == WeaponType.Melee)
			return true;

		// Las armas de rango necesitan munición
		return currentAmmo > 0;
	}
}
