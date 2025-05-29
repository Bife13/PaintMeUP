using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.SceneManagement;


[RequireComponent(typeof(TouchHandler))]
[RequireComponent(typeof(ColorHandler))]
[RequireComponent(typeof(UIManager))]
public class GameManager : MonoBehaviour
{
	[Header("Important Components")]
	[SerializeField]
	private TouchHandler m_TouchHandler;

	public TouchHandler touchHandler => m_TouchHandler;

	[SerializeField]
	private ColorHandler m_ColorHandler;

	public ColorHandler colorHandler => m_ColorHandler;

	[SerializeField]
	private UIManager m_UIManager;

	public UIManager uiManager => m_UIManager;

	[SerializeField]
	private ARSession m_ARSession;

	[Space]
	[Header("Debug Plane variables")]
	[SerializeField]
	ARPlaneManager m_PlaneManager;

	[SerializeField]
	GameObject m_DebugPlane;

	bool m_CanVisualizeNewSurfaces;

	public bool canVisualizeNewSurfaces
	{
		set => m_CanVisualizeNewSurfaces = value;
	}

	readonly List<ARFeatheredPlaneMeshVisualizerCompanion> featheredPlaneMeshVisualizerCompanions = new();

	[Space]
	[Header("Game variables")]
	[SerializeField]
	int m_RoundDuration = 30;

	[SerializeField]
	int m_TotalRounds = 6;

	private Coroutine m_TimerCoroutine;

	public Coroutine timerCoroutine
	{
		set => m_TimerCoroutine = value;
	}

	private int m_RemainingRoundDuration = 0;
	private int m_ColorCounter = 0;


	private void Awake()
	{
		// Setting up components
		m_TouchHandler.gameManager = this;
		m_UIManager.gameManager = this;
		m_ColorHandler.gameManager = this;
	}

	public void Start()
	{
		// To view the initial planes to place objects
		m_CanVisualizeNewSurfaces = true;
		m_PlaneManager.planePrefab = m_DebugPlane;
	}

	public void StartGame()
	{
		m_UIManager.HideStartPrompt();
		m_TouchHandler.hasGameStarted = true;
		m_RemainingRoundDuration = m_RoundDuration;
		m_UIManager.UpdateRandomColor();
	}

	public void CheckWin()
	{
		m_UIManager.UpdateScoreImages(m_ColorCounter);
		m_ColorCounter++;
		if (m_ColorCounter < m_TotalRounds)
		{
			m_UIManager.UpdateRandomColor();
			RestartTimer();
		}
		else
		{
			StopCoroutine(m_TimerCoroutine);
			m_TouchHandler.isGameOver = true;
			m_UIManager.ShowEndGamePrompt(EndResult.Win);
		}
	}

	public void RestartTimer()
	{
		StopCoroutine(m_TimerCoroutine);
		m_RemainingRoundDuration = m_RoundDuration;
		m_TimerCoroutine = StartCoroutine(HandleTimer());
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
					visualizerCompanion.visualizeSurfaces = m_CanVisualizeNewSurfaces;
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
					visualizer.visualizeSurfaces = m_CanVisualizeNewSurfaces;
				}
			}
		}
	}

	public void ChangePlaneVisibility(bool isVisible)
	{
		var count = featheredPlaneMeshVisualizerCompanions.Count;
		for (int i = 0; i < count; ++i)
		{
			featheredPlaneMeshVisualizerCompanions[i].visualizeSurfaces = isVisible;
		}
	}

	public IEnumerator HandleTimer()
	{
		yield return new WaitForSeconds(0.5f);

		while (m_RemainingRoundDuration >= 0)
		{
			m_UIManager.UpdateTimer(m_RemainingRoundDuration.ToString());
			yield return new WaitForSeconds(1);
			m_RemainingRoundDuration--;
		}

		yield return new WaitForSeconds(0.5f);
		uiManager.ShowEndGamePrompt(EndResult.Lose);
	}


	public void RestartGame()
	{
#if UNITY_ANDROID
		StartCoroutine(RestartRoutine());
#endif
	}

	private IEnumerator RestartRoutine()
	{
		m_ARSession.Reset();
		yield return null;
		yield return new WaitForSeconds(0.1f);
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}