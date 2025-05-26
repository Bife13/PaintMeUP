using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
	private enum SpawnTriggerType
	{
		SelectAttempt,
		InputAction,
	}

	[SerializeField]
	public GameObject startObject;

	[SerializeField]
	ARPlaneManager m_PlaneManager;


	[SerializeField]
	GameObject m_DebugPlane;


	[SerializeField]
	ObjectSpawner m_ObjectSpawner;


	[SerializeField]
	XRRayInteractor m_ARInteractor;

	[SerializeField]
	XRInputButtonReader m_SpawnObjectInput = new XRInputButtonReader("Spawn Object");


	[SerializeField]
	SpawnTriggerType m_SpawnTriggerType;

	[SerializeField]
	TMP_Text m_CounterText;

	[SerializeField]
	Image m_RandomColorImage;

	[SerializeField]
	Image m_PickedColorImage;

	[SerializeField]
	int m_RoundDuration = 30;

	private int m_RemainingRoundDuration = 0;
	private int m_ColorCounter = 0;

	readonly List<ARFeatheredPlaneMeshVisualizerCompanion> featheredPlaneMeshVisualizerCompanions = new();

	bool m_AttemptSpawn;
	bool m_GameStarted;
	bool m_EverHadSelection;
	bool m_SpawnedObject;

	public void Start()
	{
		m_PlaneManager.planePrefab = m_DebugPlane;
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public void StartGame()
	{
		startObject.SetActive(false);
		m_GameStarted = true;
		m_SpawnedObject = false;
		m_RemainingRoundDuration = m_RoundDuration;

		m_RandomColorImage.color = GenerateRandomColor();

		ChangePlaneVisibility(false);
	}


	public void Update()
	{
		if (!m_GameStarted)
			return;
#if UNITY_EDITOR || UNITY_STANDALONE

		if (m_ARInteractor.logicalSelectState.wasCompletedThisFrame)
			HandleTouchInput();

#else
		if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
		{
			HandleTouchInput();
		}
#endif
		// if (m_GameStarted)
		// {
		// 	// Spawn the initial Object
		// 	if (m_AttemptSpawn && !m_SpawnedObject)
		// 	{
		// 		m_AttemptSpawn = false;
		//
		// 		if (m_ARInteractor.hasSelection)
		// 			return;
		//
		// 		// Check if over UI
		// 		var isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
		// 		if (!isPointerOverUI && m_ARInteractor.TryGetCurrentARRaycastHit(out var arRaycastHit))
		// 		{
		// 			if (!(arRaycastHit.trackable is ARPlane arPlane))
		// 				return;
		//
		// 			if (arPlane.alignment != PlaneAlignment.HorizontalUp)
		// 				return;
		// 			if (m_ObjectSpawner.TrySpawnObject(arRaycastHit.pose.position, arPlane.normal))
		// 			{
		// 				m_SpawnedObject = true;
		// 				StartCoroutine(HandleTimer());
		// 			}
		// 		}
		// 	}
		//
		// 	var selectState = m_ARInteractor.logicalSelectState;
		//
		// 	if (selectState.wasPerformedThisFrame)
		// 		m_EverHadSelection = m_ARInteractor.hasSelection;
		// 	else if (selectState.active)
		// 		m_EverHadSelection |= m_ARInteractor.hasSelection;
		//
		// 	m_AttemptSpawn = false;
		//
		// 	if (selectState.wasCompletedThisFrame)
		// 		m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
		// }
	}

	private void HandleTouchInput()
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
			return;
#else
		//IF touch above UI
		if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
			return;
#endif

		if (!m_SpawnedObject)
		{
			if (m_ARInteractor.TryGetCurrentARRaycastHit(out var arRaycastHit))
			{
				if (!(arRaycastHit.trackable is ARPlane arPlane) || arPlane.alignment != PlaneAlignment.HorizontalUp)
					return;

				if (m_ObjectSpawner.TrySpawnObject(arRaycastHit.pose.position, arPlane.normal))
				{
					m_SpawnedObject = true;
					StartCoroutine(HandleTimer());
				}
			}
		}
		else
		{
			Debug.Log("PICK a color");
		}
	}


	public void OnEnable()
	{
		m_PlaneManager.trackablesChanged.AddListener(OnPlaneChanged);
	}

	private void OnDisable()
	{
		m_PlaneManager.trackablesChanged.RemoveListener(OnPlaneChanged);
	}

	void OnPlaneChanged(ARTrackablesChangedEventArgs<ARPlane> eventArgs)
	{
		if (eventArgs.added.Count > 0)
		{
			foreach (var plane in eventArgs.added)
			{
				if (plane.TryGetComponent<ARFeatheredPlaneMeshVisualizerCompanion>(out var visualizerCompanion))
				{
					featheredPlaneMeshVisualizerCompanions.Add(visualizerCompanion);
					visualizerCompanion.visualizeSurfaces = false;
				}
			}
		}

		if (eventArgs.removed.Count > 0)
		{
			foreach (var plane in eventArgs.removed)
			{
				if (plane.Value != null &&
				    plane.Value.TryGetComponent<ARFeatheredPlaneMeshVisualizerCompanion>(out var visualizerCompanion))
					featheredPlaneMeshVisualizerCompanions.Remove(visualizerCompanion);
			}
		}

		// Fallback if the counts do not match after an update
		if (m_PlaneManager.trackables.count != featheredPlaneMeshVisualizerCompanions.Count)
		{
			featheredPlaneMeshVisualizerCompanions.Clear();
			foreach (var trackable in m_PlaneManager.trackables)
			{
				if (trackable.TryGetComponent<ARFeatheredPlaneMeshVisualizerCompanion>(out var visualizer))
				{
					featheredPlaneMeshVisualizerCompanions.Add(visualizer);
					visualizer.visualizeSurfaces = false;
				}
			}
		}
	}

	void ChangePlaneVisibility(bool setVisible)
	{
		var count = featheredPlaneMeshVisualizerCompanions.Count;
		for (int i = 0; i < count; ++i)
		{
			featheredPlaneMeshVisualizerCompanions[i].visualizeSurfaces = setVisible;
		}
	}

	private IEnumerator HandleTimer()
	{
		while (m_ColorCounter <= 6)
		{
			while (m_RemainingRoundDuration >= 0)
			{
				m_CounterText.text = m_RemainingRoundDuration.ToString();
				yield return new WaitForSeconds(1);
				m_RemainingRoundDuration--;
			}

			yield return new WaitForSeconds(1);
			//LOST!!
		}
	}

	public void RestartTimer()
	{
		m_ColorCounter++;
		m_RemainingRoundDuration = m_RoundDuration;
	}

	public Color GenerateRandomColor()
	{
		return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
	}
}