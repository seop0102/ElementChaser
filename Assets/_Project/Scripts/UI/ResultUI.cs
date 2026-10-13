using TMPro;
using UnityEngine;

public class ResultUI : MonoBehaviour
{
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text winnerNamesText;

    private bool wasFinished = false;

    private void Update()
    {
        GameStateManager gameState = GameStateManager.Instance;

        if (gameState == null)
            return;

        bool finished =
            gameState.CurrentPhase == GamePhase.Finished;

        // 결과 화면 표시
        if (resultPanel != null &&
            resultPanel.activeSelf != finished)
        {
            resultPanel.SetActive(finished);
        }

        if (finished && winnerNamesText != null)
        {
            winnerNamesText.text =
                "WINNER\n\n" + gameState.WinnerNames;
        }

        // 게임이 종료되는 순간 한 번 실행
        if (finished && !wasFinished)
        {
            // 마우스 잠금 해제
            Cursor.lockState = CursorLockMode.None;

            // 마우스 커서 표시
            Cursor.visible = true;

            Debug.Log("[ResultUI] 게임 종료 - 마우스 커서 활성화");
        }

        wasFinished = finished;
    }

    // MAIN MENU 버튼
    public void OnClickMainMenu()
    {
        Debug.Log("[ResultUI] MAIN MENU 버튼 클릭됨!");

        if (SteamLobbyManager.Instance == null)
        {
            Debug.LogError(
                "[ResultUI] SteamLobbyManager를 찾을 수 없습니다."
            );
            return;
        }

        SteamLobbyManager.Instance.LeaveLobby();
    }
}