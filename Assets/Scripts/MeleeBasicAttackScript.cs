using UnityEngine;
using System.Collections.Generic;

public class PlayerMeleeAttack : MonoBehaviour
{
	[Header("Attack Settings")]
	[SerializeField] private float attackDamage = 10f;
	[SerializeField] private float attackRange = 1.2f;
	[SerializeField] private float attackCooldown = 0.5f;
	[SerializeField] private LayerMask enemyLayer;

	[Header("Attack Point")]
	[SerializeField] private Transform attackPoint; // Punto delante del player

	[Header("Input")]
	[SerializeField] private KeyCode attackKey = KeyCode.Mouse0; // Click izquierdo

	[Header("Visual Feedback")]
	[SerializeField] private bool showAttackGizmo = true;
	[SerializeField] private Color gizmoColor = Color.red;

	private float nextAttackTime = 0f;
	private WeaponInventory weaponInventory;

	private void Start()
	{
		weaponInventory = GetComponent<WeaponInventory>();

		// Crear AttackPoint si no existe
		if (attackPoint == null)
		{
			GameObject attackPointObj = new GameObject("AttackPoint");
			attackPointObj.transform.SetParent(transform);
			attackPointObj.transform.localPosition = new Vector3(0.8f, 0, 0); // Delante del player
			attackPoint = attackPointObj.transform;
			Debug.Log("AttackPoint created automatically");
		}
	}

	private void Update()
	{
		HandleAttackInput();
	}

	private void HandleAttackInput()
	{
		// Solo atacar con puños si NO hay arma equipada
		if (Input.GetKeyDown(attackKey) || Input.GetMouseButtonDown(0))
		{
			// Verificar si tiene arma equipada
			if (weaponInventory != null && weaponInventory.GetCurrentWeapon() != null)
			{
				// Si tiene arma, el WeaponInventory maneja el ataque
				return;
			}

			// Si no tiene arma, atacar con puños
			TryPunchAttack();
		}
	}

	private void TryPunchAttack()
	{
		// Verificar cooldown
		if (Time.time < nextAttackTime)
			return;

		// Actualizar cooldown
		nextAttackTime = Time.time + attackCooldown;

		// Ejecutar ataque
		PerformPunchAttack();
	}

	private void PerformPunchAttack()
	{
		Debug.Log("Punch attack!");

		// Detectar enemigos en rango
		Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);

		// Evitar golpear al mismo enemigo múltiples veces
		HashSet<GameObject> damagedEnemies = new HashSet<GameObject>();

		int enemiesHit = 0;

		foreach (Collider col in hitEnemies)
		{
			if (damagedEnemies.Contains(col.gameObject))
				continue;

			// Buscar componente EnemyBase
			EnemyBase enemy = col.GetComponent<EnemyBase>();

			if (enemy != null && !enemy.IsDead())
			{
				// Hacer daño
				enemy.TakeDamage(Mathf.RoundToInt(attackDamage));
				damagedEnemies.Add(col.gameObject);
				enemiesHit++;

				Debug.Log($"Punched {enemy.name} for {attackDamage} damage!");

				Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
				if (enemyRb != null)
				{
					Vector3 pushDirection = (enemy.transform.position - transform.position).normalized;
					enemyRb.AddForce(pushDirection * 5f, ForceMode.Impulse);
				}

				// Aquí puedes añadir:
				// - Efecto de sonido de golpe
				// - Partículas de impacto
				// - Empujar al enemigo
			}
		}

		if (enemiesHit == 0)
		{
			Debug.Log("Punch missed!");
		}

		// Aquí puedes añadir:
		// - Animación de puño
		// - Sonido de ataque
	}

	// Visualizar área de ataque en el editor
	private void OnDrawGizmosSelected()
	{
		if (!showAttackGizmo || attackPoint == null)
			return;

		Gizmos.color = gizmoColor;
		Gizmos.DrawWireSphere(attackPoint.position, attackRange);
	}

	// Método público para forzar un ataque (útil para animaciones)
	public void ForceAttack()
	{
		if (Time.time >= nextAttackTime)
		{
			PerformPunchAttack();
			nextAttackTime = Time.time + attackCooldown;
		}
	}

	// Getters
	public bool CanAttack()
	{
		return Time.time >= nextAttackTime;
	}

	public float GetAttackCooldownProgress()
	{
		float timeSinceLastAttack = Time.time - (nextAttackTime - attackCooldown);
		return Mathf.Clamp01(timeSinceLastAttack / attackCooldown);
	}
}
