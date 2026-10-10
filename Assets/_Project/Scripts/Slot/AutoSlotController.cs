using System;
using UnityEngine;

// 일정 간격으로 슬롯을 추첨하고 결과를 전달합니다.
public class AutoSlotController : MonoBehaviour
{
    [SerializeField]
    private SlotProbabilityTable _probabilityTable;

    // 추첨 사이의 간격입니다.
    [SerializeField, Min(0.1f)]
    private float _spinInterval = 3f;

    private SlotRoller _slotRoller;
    private double _nextSpinTime;

    // 결과를 받을 코드가 구독하는 이벤트입니다.
    public event Action<SlotResult> SlotResultGenerated;

    // 활성화되면 자동 추첨을 준비합니다.
    private void OnEnable()
    {
        if (_probabilityTable == null)
        {
            Debug.LogError(
                "슬롯 확률표를 연결해주세요.",
                this);

            enabled = false;
            return;
        }

        if (!IsIntervalValid())
        {
            enabled = false;
            return;
        }

        _slotRoller = new SlotRoller(_probabilityTable);

        // 첫 추첨은 활성화된 시점에서 3초 후 실행합니다.
        _nextSpinTime = Time.timeAsDouble + _spinInterval;
    }

    // 실행 시점에 도달하면 한 번 추첨합니다.
    private void Update()
    {
        if (Time.timeAsDouble < _nextSpinTime)
        {
            return;
        }

        if (!IsIntervalValid())
        {
            enabled = false;
            return;
        }

        // 프레임 지연 후에도 여러 회차를 한꺼번에 실행하지 않습니다.
        _nextSpinTime = Time.timeAsDouble + _spinInterval;

        GenerateSlotResult();
    }

    // 결과를 추첨하고 출력한 뒤 전달합니다.
    private void GenerateSlotResult()
    {
        SlotResult result;

        try
        {
            result = _slotRoller.Roll();
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError(exception.Message, this);
            enabled = false;
            return;
        }

        // 추첨 결과를 확인
        Debug.Log(
            $"[슬롯 추첨] "
            + $"{result.First} / "
            + $"{result.Second} / "
            + $"{result.Third}",
            this);

        SlotResultGenerated?.Invoke(result);
    }

    // 실행 간격이 유효한지 검사합니다.
    private bool IsIntervalValid()
    {
        if (float.IsNaN(_spinInterval) || float.IsInfinity(_spinInterval) || _spinInterval <= 0f)
        {
            Debug.LogError(
                "슬롯 실행 간격은 유한한 양수여야 합니다.",
                this);

            return false;
        }

        return true;
    }
}