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
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Management;
using Random = UnityEngine.Random;
using TouchPhase = UnityEngine.TouchPhase;

public class GameManager : MonoBehaviour
{
	[SerializeField]
	GameObject startObject;

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
	TMP_Text m_CounterText;

	[SerializeField]
	ARCameraManager m_ARCameraManager;

	[SerializeField]
	Image m_RandomColorImage;

	[SerializeField]
	Image m_PickedColorImage;

	[SerializeField]
	int m_RoundDuration = 30;

	private int m_RemainingRoundDuration = 0;
	private int m_ColorCounter = 0;

	[SerializeField]
	int m_TotalRounds = 6;

	readonly List<ARFeatheredPlaneMeshVisualizerCompanion> featheredPlaneMeshVisualizerCompanions = new();

	bool m_HasGameStarted;
	bool m_HasSpawnedObject;
	bool m_CanVisualizeNewSurfaces;


	[SerializeField]
	float m_ColorTolerance = 0.01f;

	Coroutine m_TimerCoroutine;

	[SerializeField]
	private HorizontalLayoutGroup m_ScoreHorizontalLayoutGroup;

	List<Image> m_ScoreImages = new List<Image>();

	[SerializeField]
	private Sprite m_CheckSprite;

	private MeshRenderer m_SpawnedObjectRenderer;

	[SerializeField]
	private GameObject m_WinPrompt;

	[SerializeField]
	private GameObject m_LosePrompt;

	[SerializeField]
	private ARSession m_ARSession;

	[SerializeField]
	private TMP_Text m_TestText;

	public void Start()
	{
		m_CanVisualizeNewSurfaces = true;
		m_PlaneManager.planePrefab = m_DebugPlane;

		GetScoreImages();
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public void StartGame()
	{
		startObject.SetActive(false);
		m_HasGameStarted = true;
		m_RemainingRoundDuration = m_RoundDuration;

		m_RandomColorImage.color = GenerateRandomColor();
	}


	public void Update()
	{
		if (!m_HasGameStarted)
			return;
#if UNITY_EDITOR || UNITY_STANDALONE

		if (m_ARInteractor.logicalSelectState.wasCompletedThisFrame)
			HandleTouchInput(new Vector2(0, 0));

#elif UNITY_ANDROID
		if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
		{
			m_TestText.text = "touched" + Random.Range(0f, 100f);
			HandleTouchInput(Touchscreen.current.primaryTouch.position.value);
		}
		// m_TestText.text = "looking for touch" + Random.Range(0f, 100f);
		// if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
		// {
		// 	m_TestText.text = "touched" + Random.Range(0f, 100f);
		// 	HandleTouchInput(Input.GetTouch(0).position);
		// }
#endif
	}

	private void HandleTouchInput(Vector2 touchPosition)
	{
#if UNITY_EDITOR || UNITY_STANDALONE
// 		if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(-1))
// 			return;
// #else
// 		//IF touch above UI
// 		if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
// 		{
// 			m_TestText.text = "above UI";
// 			return;
// 		}
#endif

		if (!m_HasSpawnedObject)
		{
			if (m_ARInteractor.TryGetCurrentARRaycastHit(out var arRaycastHit))
			{
				if (!(arRaycastHit.trackable is ARPlane arPlane)
				    // || arPlane.alignment != PlaneAlignment.HorizontalUp
				   )
					return;

				if (m_ObjectSpawner.TrySpawnObject(arRaycastHit.pose.position, arPlane.normal))
				{
					m_TestText.text = "spawned";

					m_HasSpawnedObject = true;
					ChangePlaneVisibility(false);
					m_CanVisualizeNewSurfaces = false;
					m_TimerCoroutine = StartCoroutine(HandleTimer());
					m_SpawnedObjectRenderer = m_ObjectSpawner.transform.GetComponentInChildren<MeshRenderer>();
				}
			}
		}
		else
		{
			Debug.Log("PICK a color");
			TryGetColorAtTouch(touchPosition);
		}
	}

	public void TryGetColorAtTouch(Vector2 screenPosition)
	{
		if (m_ARCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
		{
			StartCoroutine(ProcessCameraImage(image, screenPosition));
		}
	}

	IEnumerator ProcessCameraImage(XRCpuImage image, Vector2 screenPosition)
	{
		using (image)
		{
			var conversionParams = new XRCpuImage.ConversionParams
			{
				inputRect = new RectInt(0, 0, image.width, image.height),
				outputDimensions = new Vector2Int(image.width, image.height),
				outputFormat = TextureFormat.RGBA32,
				transformation = XRCpuImage.Transformation.MirrorY
			};

			Texture2D texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
			var rawTextureData = texture.GetRawTextureData<byte>();
			image.Convert(conversionParams, rawTextureData);
			texture.Apply();

			Vector2 normalized = new Vector2(screenPosition.x / Screen.width, screenPosition.y / Screen.height);
			int texX = Mathf.Clamp((int)(normalized.x * texture.width), 0, texture.width - 1);
			int texY = Mathf.Clamp((int)(normalized.y * texture.height), 0, texture.height - 1);

			Color clickedColor = texture.GetPixel(texX, texY);
			if (clickedColor != Color.clear)
			{
				m_PickedColorImage.color = clickedColor;
				if (IsSimilarColor(m_RandomColorImage.color, clickedColor))
				{
					PaintObject(clickedColor);
					CheckWin();
				}
			}

			Debug.Log("Color at Touch:" + clickedColor);

			yield return null;
		}
	}

	void CheckWin()
	{
		m_ScoreImages[m_ColorCounter].sprite = m_CheckSprite;
		m_ColorCounter++;
		if (m_ColorCounter < m_TotalRounds)
		{
			RestartTimer();
			Debug.Log("Restart Timer");
		}
		else
		{
			StopCoroutine(m_TimerCoroutine);
			m_WinPrompt.SetActive(true);
			Debug.Log("WON THE GAME");
		}
	}

	public void RestartTimer()
	{
		StopCoroutine(m_TimerCoroutine);
		m_RemainingRoundDuration = m_RoundDuration;
		m_TimerCoroutine = StartCoroutine(HandleTimer());
	}

	void PaintObject(Color color)
	{
		Debug.Log(m_SpawnedObjectRenderer.gameObject.name);
		foreach (var name in m_SpawnedObjectRenderer.material.GetTexturePropertyNames())
		{
			Debug.Log("Property: " + name);
		}

		m_SpawnedObjectRenderer.materials[0].SetColor("_BaseColor", color);
	}

	bool IsSimilarColor(Color a, Color b)
	{
		return Math.Abs(a.r - b.r) < m_ColorTolerance &&
		       Math.Abs(a.g - b.g) < m_ColorTolerance &&
		       Math.Abs(a.b - b.b) < m_ColorTolerance &&
		       Math.Abs(a.a - b.a) < m_ColorTolerance;
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

	void ChangePlaneVisibility(bool isVisible)
	{
		var count = featheredPlaneMeshVisualizerCompanions.Count;
		for (int i = 0; i < count; ++i)
		{
			featheredPlaneMeshVisualizerCompanions[i].visualizeSurfaces = isVisible;
		}
	}

	private IEnumerator HandleTimer()
	{
		yield return new WaitForSeconds(0.5f);

		while (m_RemainingRoundDuration >= 0)
		{
			m_CounterText.text = m_RemainingRoundDuration.ToString();
			yield return new WaitForSeconds(1);
			m_RemainingRoundDuration--;
		}

		yield return new WaitForSeconds(0.5f);
		m_LosePrompt.SetActive(true);
	}


	public Color GenerateRandomColor()
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		return new Color(0.392f, 0.392f, 0.392f);

#else
		return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
#endif
	}

	public void GetScoreImages()
	{
		Transform parentTransform = m_ScoreHorizontalLayoutGroup.transform;

		foreach (Transform child in parentTransform)
		{
			m_ScoreImages.Add(child.GetComponent<Image>());
		}
	}

	public void RestartGame()
	{
		StartCoroutine(RestartRoutine());
	}

	private IEnumerator RestartRoutine()
	{
		// Disable AR session to clear tracking state
		m_ARSession.Reset();

		// Wait one frame to ensure reset takes effect
		yield return null;

		// Optionally wait a bit longer to ensure session reset fully
		yield return new WaitForSeconds(0.1f);

		// Reload the scene
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}
}