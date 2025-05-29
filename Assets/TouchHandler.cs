using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class TouchHandler : MonoBehaviour
{
	[Header("Important Components")]
	[SerializeField]
	XRRayInteractor m_ARInteractor;

	[SerializeField]
	ObjectSpawner m_ObjectSpawner;

	private GameManager m_GameManager;

	public GameManager gameManager
	{
		set => m_GameManager = value;
	}

	bool m_HasSpawnedObject;
	bool m_HasGameStarted;

	public bool hasGameStarted
	{
		set => m_HasGameStarted = value;
	}

	bool m_IsGameOver;

	public bool isGameOver
	{
		set => m_IsGameOver = value;
	}

	public void Update()
	{
		if (!m_HasGameStarted || m_IsGameOver)
			return;
#if UNITY_EDITOR || UNITY_STANDALONE
		if (m_ARInteractor.logicalSelectState.wasCompletedThisFrame)
			HandleTouchInput(new Vector2(Screen.width / 2, Screen.height / 2));
#endif
#if UNITY_ANDROID
		if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
		{
			HandleTouchInput(Touchscreen.current.primaryTouch.position.value);
		}
#endif
	}

	private void HandleTouchInput(Vector2 touchPosition)
	{
		if (!m_HasSpawnedObject)
		{
			// Spawn the initial object
			if (m_ARInteractor.TryGetCurrentARRaycastHit(out var arRaycastHit))
			{
				if (!(arRaycastHit.trackable is ARPlane arPlane) || arPlane.alignment != PlaneAlignment.HorizontalUp)
					return;

				if (m_ObjectSpawner.TrySpawnObject(arRaycastHit.pose.position, arPlane.normal))
				{
					m_HasSpawnedObject = true;
					m_GameManager.ChangePlaneVisibility(false);
					m_GameManager.canVisualizeNewSurfaces = false;
					m_GameManager.timerCoroutine = StartCoroutine(m_GameManager.HandleTimer());
					m_GameManager.colorHandler.spawnedObjectRenderer =
						m_ObjectSpawner.transform.GetComponentInChildren<MeshRenderer>();
				}
			}
		}
		else
		{
			// Check the color on touch
			m_GameManager.colorHandler.TryGetColorAtTouch(touchPosition);
		}
	}
}