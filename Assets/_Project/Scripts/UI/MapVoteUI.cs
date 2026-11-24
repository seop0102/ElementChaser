using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapVoteUI : MonoBehaviour
{
    [Header("Vote Buttons")]
    [SerializeField] private Button squareButton;
    [SerializeField] private Button triangleButton;
    [SerializeField] private Button circleButton;

    [Header("Vote Count Texts")]
    [SerializeField] private TMP_Text squareVoteText;
    [SerializeField] private TMP_Text triangleVoteText;
    [SerializeField] private TMP_Text circleVoteText;

    [Header("Status Texts")]
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;

    private bool hasVoted = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        squareButton.onClick.AddListener(VoteSquare);
        triangleButton.onClick.AddListener(VoteTriangle);
        circleButton.onClick.AddListener(VoteCircle);

        SetButtonsInteractable(false);
    }

    private void Update()
    {
        MapVoteManager manager = MapVoteManager.Instance;

        if (manager == null ||
            !NetworkClient.active ||
            manager.netId == 0 ||
            !manager.VotingStarted)
        {
            timerText.text = "--";
            statusText.text = "Waiting for players...";
            SetButtonsInteractable(false);
            return;
        }

        squareVoteText.text =
            manager.SquareVotes + " Votes";

        triangleVoteText.text =
            manager.TriangleVotes + " Votes";

        circleVoteText.text =
            manager.CircleVotes + " Votes";

        if (manager.VotingFinished)
        {
            timerText.text = "0";

            statusText.text =
                MapVoteManager.GetMapName(
                    manager.SelectedMap
                ) + " SELECTED!";

            SetButtonsInteractable(false);
            return;
        }

        double remaining =
            manager.VoteEndTime - NetworkTime.time;

        int seconds = Mathf.CeilToInt(
            Mathf.Max(0f, (float)remaining)
        );

        timerText.text = seconds.ToString();

        statusText.text =
            $"Voted: {manager.TotalVotes}/4";

        SetButtonsInteractable(
            !hasVoted && remaining > 0 &&
            NetworkClient.ready &&
            NetworkClient.localPlayer != null
        );
    }

    private void VoteSquare()
    {
        SubmitVote(0);
    }

    private void VoteTriangle()
    {
        SubmitVote(1);
    }

    private void VoteCircle()
    {
        SubmitVote(2);
    }

    private void SubmitVote(int mapIndex)
    {
        MapVoteManager manager = MapVoteManager.Instance;

        if (hasVoted ||
            manager == null ||
            !manager.VotingStarted ||
            manager.VotingFinished ||
            NetworkTime.time >= manager.VoteEndTime ||
            !NetworkClient.ready ||
            NetworkClient.localPlayer == null)
            return;

        hasVoted = true;

        manager.Vote(mapIndex);

        SetButtonsInteractable(false);
    }

    private void SetButtonsInteractable(bool value)
    {
        squareButton.interactable = value;
        triangleButton.interactable = value;
        circleButton.interactable = value;
    }

    private void OnDestroy()
    {
        if (squareButton != null)
            squareButton.onClick.RemoveListener(VoteSquare);

        if (triangleButton != null)
            triangleButton.onClick.RemoveListener(VoteTriangle);

        if (circleButton != null)
            circleButton.onClick.RemoveListener(VoteCircle);
    }
}