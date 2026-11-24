using System.Collections;
using System.Collections.Generic;
using ArrowSlide.Model;
using PuzzlePair;
using PuzzlePair.Audio;
using PuzzlePair.Feedback;
using PuzzlePair.Fx;
using PuzzlePair.Input;
using PuzzlePair.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowSlide
{
    /// <summary>
    /// Game flow of Arrow Slide: loads a level into an <see cref="ArrowSession"/>, turns taps into
    /// moves and routes every outcome to the view, HUD, sound, haptics and effects.
    /// All rules live in the model; this class only orchestrates.
    /// </summary>
    public sealed class ArrowSlideGame : MonoBehaviour
    {
        public const string GameId = "arrowslide";

        [SerializeField] private ArrowBoardView _board;
        [SerializeField] private CameraRig _rig;
        [SerializeField] private TopBar _topBar;
        [SerializeField] private HeartsView _hearts;
        [SerializeField] private Button _hintButton;
        [SerializeField] private TMP_Text _hintCount;
        [SerializeField] private Button _restartButton;
        [SerializeField] private TMP_Text _remaining;
        [SerializeField] private ResultPanel _winPanel;
        [SerializeField] private ResultPanel _losePanel;
        [SerializeField] private TapPrompt _tapPrompt;

        private readonly List<Vector2> _presses = new List<Vector2>();
        private ArrowSession _session;
        private int _levelIndex;
        private bool _inputEnabled;
        private float _lastEscapeTime = -10f;
        private int _combo;
        private Coroutine _flow;

        public ArrowSession Session => _session;

        /// <summary>True while the board is waiting for taps (not during intro or result screens).</summary>
        public bool AcceptsInput => _inputEnabled && _session != null && _session.State == SessionState.Playing;

        private void Start()
        {
            Application.targetFrameRate = 60;
            _hintButton.onClick.AddListener(OnHint);
            _restartButton.onClick.AddListener(() => LoadLevel(_levelIndex));
            LoadLevel(Progress.GetLevel(GameId, ArrowLevels.All.Count));
        }

        public void LoadLevel(int index)
        {
            if (_flow != null) StopCoroutine(_flow);
            _flow = StartCoroutine(LoadRoutine(index));
        }

        private IEnumerator LoadRoutine(int index)
        {
            _inputEnabled = false;
            _levelIndex = index;
            var level = ArrowLevels.All[index];
            _session = new ArrowSession(level.CreateBoard());

            _winPanel.HideImmediate();
            _losePanel.HideImmediate();
            _tapPrompt.Hide();
            if (FxPool.Instance != null) FxPool.Instance.Clear();

            _board.Build(_session.Board);
            _rig.Fit(_board.Bounds);
            _topBar.SetLevel(index, level.Name);
            if (string.IsNullOrEmpty(level.Tip)) _topBar.HideTip();
            else _topBar.ShowTip(level.Tip);
            _hearts.SetLives(_session.Lives, false);
            RefreshHud();

            yield return _board.PlayIntro();
            _inputEnabled = true;
            if (index == 0) ShowTutorialPrompt();
            _flow = null;
        }

        private void ShowTutorialPrompt()
        {
            int id = _session.FindHint();
            if (id < 0) return;
            var arrow = _session.Board.Arrows[id];
            var middle = arrow.Cells[arrow.Length / 2];
            _tapPrompt.Show(_board.CellToWorld(middle), _board.CellSize * 1.3f);
        }

        private void Update()
        {
            if (!_inputEnabled || _session == null || _session.State != SessionState.Playing) return;

            PointerInput.GetPresses(_presses);
            foreach (var screen in _presses)
            {
                Vector3 world = _rig.Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
                int id = _board.HitTest(world);
                if (id < 0) continue;
                TapArrow(id);
                if (_session.State != SessionState.Playing) break;
            }
        }

        /// <summary>Plays a tap on an arrow, exactly as a touch would (also used by the PlayMode tests).</summary>
        public void TapArrow(int arrowId)
        {
            if (!AcceptsInput || _board.IsBusy(arrowId)) return;
            var result = _session.Tap(arrowId);
            switch (result.Outcome)
            {
                case TapOutcome.Escaped:
                    OnEscaped(result);
                    break;
                case TapOutcome.Blocked:
                    OnBlocked(result);
                    break;
            }
        }

        private void OnEscaped(TapResult result)
        {
            _combo = Time.time - _lastEscapeTime < 0.9f ? _combo + 1 : 0;
            _lastEscapeTime = Time.time;
            Sfx.Play(SfxId.Whoosh, 1f + Mathf.Min(_combo, 8) * 0.05f);
            Haptics.Play(HapticType.Light);

            _tapPrompt.Hide();
            _board.PlayEscape(result.ArrowId, result.Move, OnArrowLeftBoard);
            RefreshHud();
            if (_remaining != null) StartCoroutine(Tween.Punch(_remaining.transform, 0.12f, 0.2f, Vector3.one));

            if (result.State == SessionState.Won) _flow = StartCoroutine(WinRoutine());
        }

        private void OnArrowLeftBoard(Vector3 point, Vector2 direction)
        {
            var fx = FxPool.Instance;
            if (fx == null) return;
            var ink = _board.Theme.Ink;
            float cell = _board.CellSize;
            fx.Flash(point, new Color(ink.r, ink.g, ink.b, 0.3f), 0.2f * cell, 1.2f * cell, 0.3f);
            fx.Burst(point, new Color(ink.r, ink.g, ink.b, 0.55f), 5, 5f * cell, 0.1f * cell, 0.35f, 0f, FxShape.Circle, -direction, 140f);
            fx.Streaks(point, direction, new Color(ink.r, ink.g, ink.b, 0.5f), 4, 26f * cell, 1.2f * cell, 0.06f * cell);
        }

        private void OnBlocked(TapResult result)
        {
            _combo = 0;
            int lives = result.LivesLeft;
            _board.PlayBump(result.ArrowId, result.Move, point =>
            {
                Sfx.Play(SfxId.Bump);
                Sfx.Play(SfxId.HeartLost);
                Haptics.Play(HapticType.Medium);
                _rig.Shake(0.45f);
                _hearts.SetLives(lives, true);
                var fx = FxPool.Instance;
                if (fx != null)
                {
                    float cell = _board.CellSize;
                    fx.Flash(point, _board.Theme.Blocked, 0.2f * cell, 1.5f * cell, 0.3f);
                    fx.Flash(point, new Color(1f, 1f, 1f, 0.8f), 0.2f * cell, 0.8f * cell, 0.12f, FxShape.Circle);
                    fx.Burst(point, _board.Theme.Blocked, 8, 6f * cell, 0.12f * cell, 0.4f, -8f, FxShape.Square);

                    // The cracked heart sheds a few red pieces.
                    Vector2 screen = _hearts.HeartScreenPoint(lives);
                    Vector3 heart = _rig.Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
                    float unit = _rig.Camera.orthographicSize * 0.05f;
                    fx.Burst(heart, new Color(0.93f, 0.27f, 0.33f), 7, unit * 9f, unit * 0.45f, 0.6f, -unit * 40f, FxShape.Block);
                }
            });

            if (result.State == SessionState.Lost) _flow = StartCoroutine(LoseRoutine());
        }

        private void OnHint()
        {
            if (!_inputEnabled || _session.State != SessionState.Playing) return;
            int id = _session.UseHint();
            if (id < 0)
            {
                StartCoroutine(Tween.Shake(_hintButton.transform, 14f, 0.3f, true));
                Sfx.Play(SfxId.Bump, 1.5f, 0.6f);
                return;
            }
            _board.ShowHint(id);
            Sfx.Play(SfxId.Hint);
            Haptics.Play(HapticType.Light);
            RefreshHud();
        }

        private void RefreshHud()
        {
            if (_hintCount != null) _hintCount.text = _session.HintsLeft.ToString();
            if (_hintButton != null) _hintButton.interactable = _session.HintsLeft > 0;
            if (_remaining != null)
            {
                int n = _session.Board.RemainingCount;
                _remaining.text = n == 1 ? "1 arrow left" : $"{n} arrows left";
            }
        }

        private IEnumerator WinRoutine()
        {
            _inputEnabled = false;
            _board.ClearHint();
            _topBar.HideTip();
            yield return new WaitForSeconds(0.5f);

            Sfx.Play(SfxId.Win);
            Haptics.Play(HapticType.Success);
            _rig.ZoomPunch(0.05f);
            _board.Celebrate();
            var fx = FxPool.Instance;
            if (fx != null)
            {
                var b = _board.Bounds;
                float size = 0.16f * _board.CellSize;
                fx.ConfettiBurst(new Vector2(b.xMin, b.yMin), 45, b.height * 1.6f, size, _board.Theme.Confetti);
                fx.ConfettiBurst(new Vector2(b.xMax, b.yMin), 45, b.height * 1.6f, size, _board.Theme.Confetti);
            }

            int count = ArrowLevels.All.Count;
            bool last = _levelIndex == count - 1;
            int next = Progress.CompleteLevel(GameId, _levelIndex, count);
            yield return new WaitForSeconds(0.55f);

            if (last)
            {
                _winPanel.Show("All Clear!", "You solved every board.\nPlay them again?", "Play Again", () => LoadLevel(next));
            }
            else
            {
                string subtitle = _session.Mistakes == 0
                    ? "Perfect - not a single bump!"
                    : $"Cleared with {_session.Mistakes} bump{(_session.Mistakes == 1 ? "" : "s")}.";
                _winPanel.Show("Level Complete!", subtitle, "Next", () => LoadLevel(next));
            }
            _flow = null;
        }

        private IEnumerator LoseRoutine()
        {
            _inputEnabled = false;
            _board.ClearHint();
            yield return new WaitForSeconds(0.75f);
            Sfx.Play(SfxId.Fail);
            Haptics.Play(HapticType.Failure);
            _losePanel.Show("Out of Hearts", "A blocked arrow costs a heart.\nCheck where each one points!", "Try Again", () => LoadLevel(_levelIndex));
            _flow = null;
        }
    }
}
