using UnityEngine;

public class WeaponBase : MonoBehaviour
{
	[Header("Weapon Properties")]
	public string weaponName = "Weapon";
	public Sprite weaponIcon; // Icono para mostrar en el HUD
	public float damage = 10f;
	public float fireRate = 0.5f; // Tiempo entre disparos
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
		currentAmmo = maxAmmo;
	}

	// Método principal para disparar (override en armas específicas)
	public virtual void Fire()
	{
		if (isReloading || Time.time < nextFireTime || currentAmmo <= 0)
			return;

		currentAmmo--;
		nextFireTime = Time.time + fireRate;

		// Implementar disparo específico en clases hijas
		Debug.Log($"{weaponName} fired! Ammo: {currentAmmo}/{maxAmmo}");
	}

	// Método para recargar
	public virtual void Reload()
	{
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
		return $"{currentAmmo}/{maxAmmo}";
	}
}
