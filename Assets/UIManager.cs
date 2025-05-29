using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	private GameManager m_GameManager;

	public GameManager gameManager
	{
		get => m_GameManager;
		set => m_GameManager = value;
	}

	[SerializeField]
	Image m_RandomColorImage;

	public Image randomColorImage => m_RandomColorImage;

	[SerializeField]
	Image m_PickedColorImage;

	public Image pickedColorImage => m_PickedColorImage;

	[SerializeField]
	private HorizontalLayoutGroup m_ScoreHorizontalLayoutGroup;

	List<Image> m_ScoreImages = new();

	[SerializeField]
	private Sprite m_CheckSprite;


	[SerializeField]
	private GameObject m_WinPrompt;

	[SerializeField]
	private GameObject m_LosePrompt;

	[SerializeField]
	GameObject m_StartPrompt;

	[SerializeField]
	TMP_Text m_CounterText;

	public void GetScoreImages()
	{
		Transform parentTransform = m_ScoreHorizontalLayoutGroup.transform;

		foreach (Transform child in parentTransform)
		{
			m_ScoreImages.Add(child.GetComponent<Image>());
		}
	}

	private void Start()
	{
		GetScoreImages();
	}

	public void UpdateScoreImages(int index)
	{
		m_ScoreImages[index].sprite = m_CheckSprite;
	}

	public void ShowEndGamePrompt(EndResult result)
	{
		switch (result)
		{
			case EndResult.Win:
				m_WinPrompt.SetActive(true);
				break;
			case EndResult.Lose:
				m_LosePrompt.SetActive(true);
				break;
		}
	}

	public void HideStartPrompt()
	{
		m_StartPrompt.SetActive(false);
	}

	public void UpdateRandomColor()
	{
		m_RandomColorImage.color = m_GameManager.colorHandler.GenerateRandomColor();
	}

	public void UpdateTimer(string value)
	{
		m_CounterText.text = value;
	}
}

public enum EndResult
{
	Win,
	Lose
}