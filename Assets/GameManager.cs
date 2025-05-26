using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

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
	float m_RoundDuration = 30f;

	private float m_RemainingRoundDuration = 0f;

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

		ChangePlaneVisibility(false);
	}


	public void Update()
	{
		// Spawn the initial Object
		if (m_AttemptSpawn && m_GameStarted && !m_SpawnedObject)
		{
			m_AttemptSpawn = false;

			if (m_ARInteractor.hasSelection)
				return;

			// Check if over UI
			var isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1);
			if (!isPointerOverUI && m_ARInteractor.TryGetCurrentARRaycastHit(out var arRaycastHit))
			{
				if (!(arRaycastHit.trackable is ARPlane arPlane))
					return;

				if (arPlane.alignment != PlaneAlignment.HorizontalUp)
					return;

				if (m_ObjectSpawner.TrySpawnObject(arRaycastHit.pose.position, arPlane.normal))
					m_SpawnedObject = true;
			}
		}

		var selectState = m_ARInteractor.logicalSelectState;

		if (selectState.wasPerformedThisFrame)
			m_EverHadSelection = m_ARInteractor.hasSelection;
		else if (selectState.active)
			m_EverHadSelection |= m_ARInteractor.hasSelection;

		m_AttemptSpawn = false;
		switch (m_SpawnTriggerType)
		{
			case SpawnTriggerType.SelectAttempt:
				if (selectState.wasCompletedThisFrame)
					m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
				break;

			case SpawnTriggerType.InputAction:
				if (m_SpawnObjectInput.ReadWasPerformedThisFrame())
					m_AttemptSpawn = !m_ARInteractor.hasSelection && !m_EverHadSelection;
				break;
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

	void HandleTimer()
	{
		if (m_RemainingRoundDuration > 0)
			m_RemainingRoundDuration -= Time.deltaTime;
	}
}