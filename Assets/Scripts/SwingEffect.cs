using UnityEngine;
using System.Collections.Generic;

public class SwingEffect : MonoBehaviour
{
	[Header("Swing Settings")]
	[SerializeField] private bool rotateWithSwing = true;

	// Parámetros del arma (set by Initialize)
	private int damage = 15;
	private LayerMask hitLayers;
	private Transform playerTransform;
	private float swingRange = 2.5f;
	private float swingAngle = 120f;
	private float swingDuration = 0.3f;

	// Estado interno
	private bool isInitialized = false;
	private float startTime;
	private float centerAngle;
	private float halfAngle;
	private HashSet<GameObject> hitEnemies = new HashSet<GameObject>();
	private Vector3 swingOrigin;

	/// <summary>
	/// Inicializa el efecto de barrido con los parámetros del arma
	/// </summary>
	public void Initialize(int swingDamage, LayerMask layers, Transform player, float range, float angle, float duration, Vector3 cursorDirection)
	{
		damage = swingDamage;
		hitLayers = layers;
		playerTransform = player;
		swingRange = range;
		swingAngle = angle;
		swingDuration = duration;
		halfAngle = angle / 2f;

		// Calcular ángulo central basado en la dirección del cursor
		centerAngle = Mathf.Atan2(cursorDirection.x, cursorDirection.z) * Mathf.Rad2Deg;

		// Guardar origen
		swingOrigin = player.position;

		// Posicionar el efecto
		transform.position = swingOrigin;
		transform.rotation = Quaternion.Euler(0, centerAngle - halfAngle, 0);

		startTime = Time.time;
		isInitialized = true;

		// Detectar enemigos inmediatamente al inicio
		DetectAllEnemiesInArc();

		// Auto-destruir después de la duración
		Destroy(gameObject, swingDuration + 0.1f);
	}

	private void Update()
	{
		if (!isInitialized || playerTransform == null) return;

		// Actualizar origen por si el jugador se mueve
		swingOrigin = playerTransform.position;
		transform.position = swingOrigin;

		// Calcular progreso del barrido
		float elapsed = Time.time - startTime;
		float progress = Mathf.Clamp01(elapsed / swingDuration);

		// Rotar el efecto con el barrido
		if (rotateWithSwing)
		{
			float currentAngle = Mathf.Lerp(centerAngle - halfAngle, centerAngle + halfAngle, progress);
			transform.rotation = Quaternion.Euler(0, currentAngle, 0);
		}

		// Detectar enemigos continuamente durante el barrido
		DetectAllEnemiesInArc();
	}

	private void DetectAllEnemiesInArc()
	{
		// Usar OverlapSphere grande para detectar todos los enemigos cercanos
		Collider[] colliders = Physics.OverlapSphere(swingOrigin, swingRange, hitLayers);

		foreach (Collider col in colliders)
		{
			// Ya golpeamos a este enemigo?
			if (hitEnemies.Contains(col.gameObject))
			{
				continue;
			}

			// Calcular dirección al enemigo
			Vector3 toEnemy = col.transform.position - swingOrigin;
			toEnemy.y = 0; // Ignorar altura
			float distanceToEnemy = toEnemy.magnitude;

			// Verificar que está dentro del rango
			if (distanceToEnemy > swingRange)
			{
				continue;
			}

			// Calcular ángulo hacia el enemigo
			float enemyAngle = Mathf.Atan2(toEnemy.x, toEnemy.z) * Mathf.Rad2Deg;

			// Verificar si está dentro del arco
			float angleDiff = Mathf.DeltaAngle(centerAngle, enemyAngle);

			if (Mathf.Abs(angleDiff) <= halfAngle)
			{
				// Está dentro del arco! Hacer daño
				TryDamageEnemy(col);
			}
		}
	}

	private void TryDamageEnemy(Collider col)
	{
		if (hitEnemies.Contains(col.gameObject))
		{
			return;
		}

		EnemyBase enemy = col.GetComponent<EnemyBase>();
		if (enemy != null && !enemy.IsDead())
		{
			enemy.TakeDamage(damage);
			hitEnemies.Add(col.gameObject);

			Debug.Log($"SwingEffect: Hit {enemy.name} for {damage} damage!");

			// Knockback
			Rigidbody enemyRb = enemy.GetComponent<Rigidbody>();
			if (enemyRb != null && playerTransform != null)
			{
				Vector3 pushDirection = (enemy.transform.position - playerTransform.position).normalized;
				enemyRb.AddForce(pushDirection * 8f, ForceMode.Impulse);
			}
		}
	}

	// Para trigger collisions (si el prefab tiene un collider trigger)
	private void OnTriggerEnter(Collider other)
	{
		// Verificar si está en las capas que puede golpear
		if (((1 << other.gameObject.layer) & hitLayers) == 0)
		{
			return;
		}

		TryDamageEnemy(other);
	}

	// Visualización en el editor
	private void OnDrawGizmos()
	{
		if (!isInitialized) return;

		Vector3 pos = swingOrigin;

		// Dibujar el arco completo
		Gizmos.color = Color.yellow;

		int segments = 20;
		Vector3 prevPoint = pos;

		for (int i = 0; i <= segments; i++)
		{
			float t = (float)i / segments;
			float angle = Mathf.Lerp(centerAngle - halfAngle, centerAngle + halfAngle, t);
			Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
			Vector3 point = pos + dir * swingRange;

			if (i > 0)
			{
				Gizmos.DrawLine(prevPoint, point);
			}
			Gizmos.DrawLine(pos, point);
			prevPoint = point;
		}
	}
}
