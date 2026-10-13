using TMPro;
using UnityEngine;

public class GameTimerUI : MonoBehaviour
{
    // 게임 진행 중 남은 시간을 MM:SS 형식으로 표시
    [SerializeField] private TMP_Text timerText;

    private void Update()
    {
        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
        {
            timerText.gameObject.SetActive(false);
            return;
        }

        // 게임 진행 중일 때만 타이머 표시
        bool isPlaying =
            gameState.CurrentPhase == GamePhase.Playing;

        timerText.gameObject.SetActive(isPlaying);

        if (!isPlaying)
            return;

        // 남은 시간을 분:초 형식으로 변환
        int totalSeconds = Mathf.CeilToInt(
            (float)gameState.RemainingTime
        );

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}