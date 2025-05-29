using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Random = UnityEngine.Random;

public class ColorHandler : MonoBehaviour
{
	[Header("Important Components")]
	[SerializeField]
	ARCameraManager m_ARCameraManager;

	[Space]
	[Header("Color detection variables")]
	[SerializeField]
	float m_ColorTolerance = 0.01f;

	[SerializeField]
	private int m_ColorRadius;

	private GameManager m_GameManager;

	public GameManager gameManager
	{
		set => m_GameManager = value;
	}

	private MeshRenderer m_SpawnedObjectRenderer;

	public MeshRenderer spawnedObjectRenderer
	{
		set => m_SpawnedObjectRenderer = value;
	}


	public void TryGetColorAtTouch(Vector2 screenPosition)
	{
		if (m_ARCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
		{
			StartCoroutine(ProcessCameraImage(image, screenPosition));
		}
	}

	IEnumerator ProcessCameraImage(XRCpuImage image, Vector2 touchPosition)
	{
		using (image)
		{
			var conversionParams = new XRCpuImage.ConversionParams
			{
				inputRect = new RectInt(0, 0, image.width, image.height),
				outputDimensions = new Vector2Int(image.width, image.height),
				outputFormat = TextureFormat.RGBA32,
				transformation = XRCpuImage.Transformation.MirrorY,
			};

			Texture2D texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
			var rawTextureData = texture.GetRawTextureData<byte>();
			image.Convert(conversionParams, rawTextureData);
			texture.Apply();

			Vector2 normalized = new Vector2(touchPosition.x / Screen.width, touchPosition.y / Screen.height);
			// Invert the X and Y because android rotates CPU image anti-clockwise
#if UNITY_ANDROID
			int texX = Mathf.Clamp((int)(normalized.y * texture.width), 0, texture.width - 1);
			int texY = Mathf.Clamp((int)((1f - normalized.x) * texture.height), 0, texture.height - 1);
#else
			int texX = Mathf.Clamp((int)(normalized.x * texture.width), 0, texture.width - 1);
			int texY = Mathf.Clamp((int)(normalized.y * texture.height), 0, texture.height - 1);
#endif

			//Get the average color around the selected pixel
			Color clickedColor = GetAverageColor(texture, texX, texY, m_ColorRadius);
			if (clickedColor != Color.clear)
			{
				m_GameManager.uiManager.pickedColorImage.color = clickedColor;
				if (IsSimilarColor(m_GameManager.uiManager.randomColorImage.color, clickedColor))
				{
					PaintObject(clickedColor);
					m_GameManager.CheckWin();
				}
			}

			yield return null;
		}
	}

	Color GetAverageColor(Texture2D texture, int centerX, int centerY, int radius = 3)
	{
		Color sum = Color.black;
		int count = 0;

		for (int dx = -radius; dx <= radius; dx++)
		{
			for (int dy = -radius; dy <= radius; dy++)
			{
				int x = Mathf.Clamp(centerX + dx, 0, texture.width - 1);
				int y = Mathf.Clamp(centerY + dy, 0, texture.height - 1);

				sum += texture.GetPixel(x, y);
				count++;
			}
		}

		return sum / count;
	}

	void PaintObject(Color color)
	{
		m_SpawnedObjectRenderer.materials[0].SetColor("_BaseColor", color);
	}

	bool IsSimilarColor(Color a, Color b)
	{
		// Check how similar the selected color is to the random color
		return Math.Abs(a.r - b.r) < m_ColorTolerance &&
		       Math.Abs(a.g - b.g) < m_ColorTolerance &&
		       Math.Abs(a.b - b.b) < m_ColorTolerance &&
		       Math.Abs(a.a - b.a) < m_ColorTolerance;
	}

	public Color GenerateRandomColor()
	{
#if UNITY_EDITOR || UNITY_STANDALONE
		return new Color(0.392f, 0.392f, 0.392f);
#else
		return new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
#endif
	}

	public void UpdateColorTolerance(float value)
	{
		m_ColorTolerance = value;
	}
}