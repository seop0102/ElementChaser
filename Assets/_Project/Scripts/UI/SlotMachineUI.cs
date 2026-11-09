using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class SlotMachineUI : MonoBehaviour
{
    [Header("슬롯 이미지: 왼쪽 / 가운데 / 오른쪽")]
    [SerializeField] private Image[] _symbols = new Image[3];

    [Header("원소 그림")]
    [SerializeField] private Sprite _fire;
    [SerializeField] private Sprite _water;
    [SerializeField] private Sprite _grass;
    [SerializeField] private Sprite _wind;

    [Header("회전")]
    [Tooltip("원소 한 칸을 지나가는 시간입니다. 작을수록 빠릅니다.")]
    [SerializeField, Min(0.02f)]
    private float _changeInterval = 0.08f;

    [Tooltip("위아래 원소의 중심 사이 거리입니다.")]
    [SerializeField, Min(1f)]
    private float _symbolSpacing = 120f;

    [Header("정지")]
    [SerializeField, Min(0.1f)]
    private float _stopDuration = 0.45f;

    [SerializeField, Min(0.05f)]
    private float _stopGap = 0.18f;

    [SerializeField, Min(0f)]
    private float _resultDisplayTime = 0.7f;

    private PlayerSlotController _controller;
    private Reel[] _reels;

    private bool _settling;
    private float _showResultUntil;

    private void Awake()
    {
        if (_symbols == null || _symbols.Length != 3 ||
            _symbols[0] == null ||
            _symbols[1] == null ||
            _symbols[2] == null ||
            _fire == null || _water == null ||
            _grass == null || _wind == null)
        {
            Debug.LogError(
                "[Slot UI] Symbol 3개와 원소 Sprite 4개를 연결해주세요.",
                this);

            enabled = false;
            return;
        }

        Sprite[] sprites = { _fire, _water, _grass, _wind };
        float speed = 1f / Mathf.Max(0.02f, _changeInterval);

        _reels = new Reel[3];

        for (int i = 0; i < _reels.Length; i++)
        {
            _reels[i] = new Reel(
                _symbols[i],
                sprites,
                _symbolSpacing,
                speed,
                i);
        }

        RestoreDisplay();
    }

    private void Update()
    {
        if (_reels == null)
            return;

        PlayerSlotController current = null;

        if (NetworkClient.localPlayer != null)
        {
            current = NetworkClient.localPlayer
                .GetComponent<PlayerSlotController>();
        }

        if (!object.ReferenceEquals(_controller, current))
            Bind(current);

        bool canSpin =
            _controller != null &&
            _controller.IsSpinning &&
            !_settling &&
            Time.unscaledTime >= _showResultUntil;

        bool anyStopping = false;

        foreach (Reel reel in _reels)
        {
            reel.Tick(Time.unscaledDeltaTime, canSpin);
            anyStopping |= reel.IsStopping;
        }

        // 三つとも止まってから結果の表示時間を数えます。
        if (_settling && !anyStopping)
        {
            _settling = false;
            _showResultUntil =
                Time.unscaledTime + _resultDisplayTime;
        }
    }

    private void Bind(PlayerSlotController controller)
    {
        Unbind();

        _controller = controller;

        if (_controller != null)
        {
            _controller.ResultReceived += OnResultReceived;
            _controller.RoundCancelled += OnRoundCancelled;
        }

        RestoreDisplay();

        // UIが途中から有効になった場合も最後の結果を表示します。
        if (_controller != null && _controller.HasResult)
        {
            _showResultUntil =
                Time.unscaledTime + _resultDisplayTime;
        }
    }

    private void Unbind()
    {
        if (!object.ReferenceEquals(_controller, null))
        {
            _controller.ResultReceived -= OnResultReceived;
            _controller.RoundCancelled -= OnRoundCancelled;
        }

        _controller = null;
    }

    private void OnResultReceived(SlotResult result)
    {
        _settling = true;

        ElementType[] elements =
        {
            result.First,
            result.Second,
            result.Third
        };

        float duration = Mathf.Max(0.1f, _stopDuration);

        for (int i = 0; i < _reels.Length; i++)
        {
            // 앞 칸보다 뒤 칸이 늦게 멈추도록 설정합니다.
            float actualDuration =
                _reels[i].StopAt(GetSprite(elements[i]), duration);

            duration = actualDuration + Mathf.Max(0.05f, _stopGap);
        }
    }

    private void OnRoundCancelled()
    {
        RestoreDisplay();
    }

    private void RestoreDisplay()
    {
        if (_reels == null)
            return;

        _settling = false;
        _showResultUntil = 0f;

        SlotResult result =
            _controller != null && _controller.HasResult
                ? _controller.LastResult
                : new SlotResult(
                    ElementType.Fire,
                    ElementType.Water,
                    ElementType.Grass);

        _reels[0].ShowImmediately(GetSprite(result.First));
        _reels[1].ShowImmediately(GetSprite(result.Second));
        _reels[2].ShowImmediately(GetSprite(result.Third));
    }

    private Sprite GetSprite(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return _fire;
            case ElementType.Water: return _water;
            case ElementType.Grass: return _grass;
            case ElementType.Wind: return _wind;
            default: return null;
        }
    }

    private void OnDisable()
    {
        RestoreDisplay();
        Unbind();
    }

    private void OnDestroy()
    {
        if (_reels == null)
            return;

        foreach (Reel reel in _reels)
            reel.Dispose();
    }

    // 슬롯 한 칸의 표시와 이동을 담당합니다.
    private sealed class Reel
    {
        private readonly Image[] _images = new Image[3];
        private readonly Sprite[] _sprites;
        private readonly Vector2 _origin;
        private readonly Vector3 _baseScale;
        private readonly Color _baseColor;

        private readonly float _spacing;
        private readonly float _maxSpeed;
        private readonly int _spriteOffset;

        private float _position;
        private float _speed;

        private float _stopStart;
        private float _stopTarget;
        private float _stopTime;
        private float _stopElapsed;

        private int _resultCell;
        private Sprite _resultSprite;

        public bool IsStopping { get; private set; }

        public Reel(
            Image source,
            Sprite[] sprites,
            float spacing,
            float speed,
            int spriteOffset)
        {
            _sprites = sprites;
            _spacing = Mathf.Max(1f, spacing);
            _maxSpeed = speed;
            _spriteOffset = spriteOffset;

            _origin = source.rectTransform.anchoredPosition;
            _baseScale = source.rectTransform.localScale;
            _baseColor = source.color;

            // 가운데에는 기존 Symbol을 그대로 사용합니다.
            _images[1] = source;

            // 보조 그림 두 개만 실행 중 생성합니다.
            _images[0] = CreateImage(source, "Symbol_RuntimeLower");
            _images[2] = CreateImage(source, "Symbol_RuntimeUpper");

            foreach (Image image in _images)
            {
                image.raycastTarget = false;
                image.maskable = true;
            }
        }

        private static Image CreateImage(Image source, string name)
        {
            GameObject obj = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            obj.layer = source.gameObject.layer;

            RectTransform rect = obj.GetComponent<RectTransform>();
            RectTransform original = source.rectTransform;

            rect.SetParent(original.parent, false);
            rect.anchorMin = original.anchorMin;
            rect.anchorMax = original.anchorMax;
            rect.pivot = original.pivot;
            rect.sizeDelta = original.sizeDelta;
            rect.anchoredPosition3D = original.anchoredPosition3D;
            rect.localRotation = original.localRotation;
            rect.localScale = original.localScale;

            Image image = obj.GetComponent<Image>();
            image.type = Image.Type.Simple;
            image.material = source.material;
            image.preserveAspect = source.preserveAspect;
            image.color = source.color;
            image.raycastTarget = false;

            return image;
        }

        public void ShowImmediately(Sprite sprite)
        {
            IsStopping = false;
            _position = 0f;
            _speed = 0f;

            _resultCell = 0;
            _resultSprite = sprite;

            Render();
        }

        public float StopAt(Sprite result, float minimumDuration)
        {
            _stopStart = _position;

            float entrySpeed =
                _speed > 0.01f ? _speed : _maxSpeed;

            // 原素の中心位置で止まるように移動先を整数にします。
            _stopTarget = Mathf.Ceil(
                _position + entrySpeed * minimumDuration / 3f);

            float distance = _stopTarget - _stopStart;

            // 감속 시작 시 이동 속도가 이어지도록 시간을 계산합니다.
            _stopTime = 3f * distance / entrySpeed;
            _stopElapsed = 0f;

            _resultCell = Mathf.RoundToInt(_stopTarget);
            _resultSprite = result;
            IsStopping = true;

            return _stopTime;
        }

        public void Tick(float deltaTime, bool spin)
        {
            if (IsStopping)
            {
                _stopElapsed += deltaTime;

                float t = Mathf.Clamp01(_stopElapsed / _stopTime);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                _position = Mathf.Lerp(
                    _stopStart,
                    _stopTarget,
                    eased);

                if (t >= 1f)
                {
                    _position = _stopTarget;
                    _speed = 0f;
                    IsStopping = false;
                }
            }
            else if (spin)
            {
                // 약 0.25초 동안 회전 속도를 올립니다.
                _speed = Mathf.MoveTowards(
                    _speed,
                    _maxSpeed,
                    _maxSpeed * 4f * deltaTime);

                _position += _speed * deltaTime;
            }
            else
            {
                _speed = 0f;
            }

            Render();
        }

        private void Render()
        {
            int centerCell = Mathf.FloorToInt(_position + 0.5f);

            for (int i = 0; i < _images.Length; i++)
            {
                int cell = centerCell + i - 1;
                float y = (cell - _position) * _spacing;

                int spriteIndex = (cell + _spriteOffset) % _sprites.Length;

                if (spriteIndex < 0)
                    spriteIndex += _sprites.Length;

                Sprite sprite = cell == _resultCell
                    ? _resultSprite
                    : _sprites[spriteIndex];

                Image image = _images[i];
                image.sprite = sprite;
                image.enabled = sprite != null;

                image.rectTransform.anchoredPosition =
                    _origin + Vector2.up * y;

                // 위아래에서는 납작하고 어둡게 보여 릴 느낌을 냅니다.
                float edge = Mathf.Clamp01(Mathf.Abs(y) / _spacing);
                float scaleY = Mathf.Lerp(1f, 0.6f, edge);
                float brightness = Mathf.Lerp(1f, 0.65f, edge);

                image.rectTransform.localScale = new Vector3(
                    _baseScale.x,
                    _baseScale.y * scaleY,
                    _baseScale.z);

                image.color = new Color(
                    _baseColor.r * brightness,
                    _baseColor.g * brightness,
                    _baseColor.b * brightness,
                    _baseColor.a);
            }
        }

        public void Dispose()
        {
            if (_images[0] != null)
                Object.Destroy(_images[0].gameObject);

            if (_images[2] != null)
                Object.Destroy(_images[2].gameObject);

            if (_images[1] != null)
            {
                _images[1].rectTransform.anchoredPosition = _origin;
                _images[1].rectTransform.localScale = _baseScale;
                _images[1].color = _baseColor;
            }
        }
    }
}