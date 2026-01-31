using UnityEngine;
using TMPro;

public class LeaderboardEntry : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI rankText;
	[SerializeField] private TextMeshProUGUI scoreText;
	[SerializeField] private TextMeshProUGUI timeText;
	[SerializeField] private TextMeshProUGUI dateText;

	public void SetData(int rank, RunData data, Color color)
	{
		if (rankText != null)
		{
			rankText.text = $"#{rank}";
			rankText.color = color;
		}

		if (scoreText != null)
		{
			scoreText.text = data.score.ToString();
			scoreText.color = color;
		}

		if (timeText != null)
		{
			timeText.text = data.timeElapsed;
			timeText.color = color;
		}

		if (dateText != null)
		{
			dateText.text = data.date;
			dateText.color = color;
		}
	}
}
