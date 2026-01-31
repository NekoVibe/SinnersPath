using UnityEngine;

/// <summary>
/// Ajusta el tamaño y posición del collider del enemigo según el frame de animación
/// Coloca este script en el mismo GameObject que tiene el Animator y el Collider
/// </summary>
public class EnemyColliderAdjuster : MonoBehaviour
{
	[System.Serializable]
	public class ColliderState
	{
		public string stateName; // Nombre del estado de animación (ej: "Idle", "Walk", "Punch")
		public Vector3 colliderCenter = Vector3.zero;
		public Vector3 colliderSize = Vector3.one;
	}

	[Header("Collider States")]
	[SerializeField] private ColliderState[] colliderStates;

	[Header("References")]
	[SerializeField] private BoxCollider boxCollider;
	[SerializeField] private Animator animator;

	[Header("Settings")]
	[SerializeField] private bool debugMode = false;

	private string currentStateName = "";

	private void Start()
	{
		// Obtener referencias si no están asignadas
		if (boxCollider == null)
			boxCollider = GetComponent<BoxCollider>();

		if (animator == null)
			animator = GetComponent<Animator>();

		if (boxCollider == null)
		{
			Debug.LogError("EnemyColliderAdjuster: No BoxCollider found!");
		}

		if (animator == null)
		{
			Debug.LogError("EnemyColliderAdjuster: No Animator found!");
		}
	}

	private void LateUpdate()
	{
		if (animator == null || boxCollider == null) return;

		// Obtener el estado actual de la animación
		AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
		string newStateName = GetStateName(stateInfo);

		// Solo actualizar si cambió el estado
		if (newStateName != currentStateName)
		{
			currentStateName = newStateName;
			UpdateCollider(currentStateName);
		}
	}

	// Obtener el nombre del estado actual
	private string GetStateName(AnimatorStateInfo stateInfo)
	{
		// Detectar por nombre del estado
		if (stateInfo.IsName("IdleOnf"))
			return "Idle";
		else if (stateInfo.IsName("walkOnf"))
			return "Walk";
		else if (stateInfo.IsName("Punch"))
			return "Punch";

		return "Idle"; // Default
	}

	// Actualizar el collider según el estado
	private void UpdateCollider(string stateName)
	{
		foreach (ColliderState state in colliderStates)
		{
			if (state.stateName == stateName)
			{
				boxCollider.center = state.colliderCenter;
				boxCollider.size = state.colliderSize;

				if (debugMode)
				{
					Debug.Log($"Collider updated to state: {stateName} - Center: {state.colliderCenter}, Size: {state.colliderSize}");
				}

				return;
			}
		}

		if (debugMode)
		{
			Debug.LogWarning($"No collider state found for: {stateName}");
		}
	}

	// Visualizar los colliders en el editor
	private void OnDrawGizmosSelected()
	{
		if (colliderStates == null || colliderStates.Length == 0) return;

		Gizmos.color = Color.cyan;
		foreach (ColliderState state in colliderStates)
		{
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(state.colliderCenter, state.colliderSize);
		}
	}
}
