using System.Collections;
using System.Collections.Generic;
using PixelParade.Model;
using PuzzlePair;
using PuzzlePair.Audio;
using PuzzlePair.Feedback;
using PuzzlePair.Fx;
using PuzzlePair.Input;
using PuzzlePair.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PixelParade
{
    /// <summary>
    /// Game flow of Pixel Parade. Runs the <see cref="PixelSim"/> on a fixed tick, turns taps into
    /// launches and translates every simulation event into animation, sound and haptics.
    /// Riders are drawn between belt nodes using the time left until the next tick, so motion is
    /// smooth at any frame rate while the rules stay deterministic.
    /// </summary>
    public sealed class PixelParadeGame : MonoBehaviour
    {
        public const string GameId = "pixelparade";
        private const int BoostersPerLevel = 1;

        [SerializeField] private PixelTheme _theme;
        [SerializeField] private CameraRig _rig;
        [SerializeField] private TopBar _topBar;
        [SerializeField] private PictureView _picture;
        [SerializeField] private BeltView _belt;
        [SerializeField] private SlotsView _slots;
        [SerializeField] private ProjectilePool _projectiles;
        [SerializeField] private Transform _shooterRoot;
        [SerializeField] private ProgressBar _progress;
        [SerializeField] private TMP_Text _beltLoad;
        [SerializeField] private Button _slotBooster;
        [SerializeField] private TMP_Text _slotBoosterCount;
        [SerializeField] private Button _restartButton;
        [SerializeField] private ResultPanel _winPanel;
        [SerializeField] private ResultPanel _losePanel;
        [SerializeField] private TapPrompt _tapPrompt;

        private readonly Dictionary<int, ShooterView> _views = new Dictionary<int, ShooterView>();
        private readonly Dictionary<int, (Vector2 from, float time)> _boarding = new Dictionary<int, (Vector2, float)>();
        private readonly List<SimEvent> _events = new List<SimEvent>();
        private readonly List<Vector2> _presses = new List<Vector2>();

        private PixelSim _sim;
        private PixelLayout _layout;
        private int _levelIndex;
        private bool _playing;
        private float _tickTimer;
        private int _boostersLeft;
        private int _popCombo;
        private float _lastPopTime;
        private Coroutine _flow;

        public PixelSim Sim => _sim;

        /// <summary>True while shooters can be launched (not during intro or result screens).</summary>
        public bool AcceptsInput => _playing && _sim != null && _sim.State == SimState.Playing;

        /// <summary>Plays a tap on a shooter, exactly as a touch would (also used by the PlayMode tests).</summary>
        public void TapShooter(int shooterId)
        {
            if (!AcceptsInput) return;
            TryLaunch(_sim.Shooters[shooterId]);
        }

        /// <summary>Uses the +1 slot booster (same as its button).</summary>
        public void UseSlotBooster() => OnSlotBooster();

        private void Start()
        {
            Application.targetFrameRate = 60;
            _projectiles.Setup(_theme.Circle, _theme.ProjectileSpeed);
            _restartButton.onClick.AddListener(() => LoadLevel(_levelIndex));
            _slotBooster.onClick.AddListener(OnSlotBooster);
            LoadLevel(Progress.GetLevel(GameId, PixelLevels.All.Count));
        }

        public void LoadLevel(int index)
        {
            if (_flow != null) StopCoroutine(_flow);
            _flow = StartCoroutine(LoadRoutine(index));
        }

        // ------------------------------------------------------------------ setup

        private IEnumerator LoadRoutine(int index)
        {
            _playing = false;
            _levelIndex = index;
            var level = PixelLevels.All[index];
            _sim = new PixelSim(level);
            _layout = new PixelLayout(_sim, _theme);
            _tickTimer = 0f;
            _boostersLeft = BoostersPerLevel;

            _winPanel.HideImmediate();
            _losePanel.HideImmediate();
            _tapPrompt.Hide();
            _projectiles.Clear();
            if (FxPool.Instance != null) FxPool.Instance.Clear();
            foreach (var view in _views.Values)
                if (view != null) Destroy(view.gameObject);
            _views.Clear();
            _boarding.Clear();

            _picture.Build(_sim.Picture, _layout, _theme);
            _belt.Build(_layout, _theme);
            _belt.Running = true;
            _slots.Build(_sim.SlotCount, _layout, _theme);
            _rig.Fit(_layout.Bounds(_sim.SlotCount + BoostersPerLevel));

            foreach (var shooter in _sim.Shooters)
            {
                var go = new GameObject($"Shooter {shooter.Id}");
                go.transform.SetParent(_shooterRoot, false);
                var view = go.AddComponent<ShooterView>();
                view.Init(shooter.Id, shooter.Color, shooter.Ammo, _theme);
                view.SetFacing(Vector2.up, true);
                _views[shooter.Id] = view;
            }
            for (int lane = 0; lane < _sim.LaneCount; lane++) ArrangeLane(lane, false);

            _topBar.SetLevel(index, level.Name);
            if (string.IsNullOrEmpty(level.Tip)) _topBar.HideTip();
            else _topBar.ShowTip(level.Tip);
            _progress.SetValue(0f, true);
            RefreshHud();

            StartCoroutine(PopShootersIn());
            yield return _picture.PlayIntro();

            _playing = true;
            if (index == 0 && _sim.LaneFront(0) != null)
                _tapPrompt.Show(_views[_sim.LaneFront(0).Id].transform.position, _theme.ShooterSize * 1.4f);
            _flow = null;
        }

        private IEnumerator PopShootersIn()
        {
            foreach (var view in _views.Values) view.transform.localScale = Vector3.zero;
            int i = 0;
            foreach (var view in _views.Values)
            {
                var t = view.transform;
                float delay = 0.15f + i++ * 0.03f;
                StartCoroutine(Tween.Delay(delay, () => StartCoroutine(Tween.Run(0.3f, k =>
                {
                    if (t != null) t.localScale = Vector3.one * Easing.OutBack(k, 2.2f);
                }))));
            }
            yield break;
        }

        /// <summary>Places a lane's shooters front to back; only the first few are shown.</summary>
        private void ArrangeLane(int lane, bool animate)
        {
            var queue = _sim.Lane(lane);
            for (int depth = 0; depth < queue.Count; depth++)
            {
                var view = _views[queue[depth].Id];
                bool visible = depth < _theme.VisiblePerLane;
                bool wasVisible = view.gameObject.activeSelf;
                view.gameObject.SetActive(visible);
                if (!visible) continue;

                Vector2 target = _layout.Lane(lane, depth);
                if (animate && wasVisible) view.MoveTo(target, 0.22f);
                else
                {
                    view.SetPosition(target);
                    if (animate) StartCoroutine(Tween.Run(0.25f, k => view.transform.localScale = Vector3.one * Easing.OutBack(k)));
                }
                view.SetIdle(depth == 0);
            }
        }

        // ------------------------------------------------------------------ loop

        private void Update()
        {
            if (_sim == null) return;

            if (_playing && _sim.State == SimState.Playing)
            {
                HandleInput();
                _tickTimer += Time.deltaTime;
                while (_tickTimer >= _theme.TickInterval && _sim.State == SimState.Playing)
                {
                    _tickTimer -= _theme.TickInterval;
                    Step();
                }
            }

            UpdateRiders(_sim.State == SimState.Playing ? Mathf.Clamp01(_tickTimer / _theme.TickInterval) : 0f);
            _progress.SetValue(_sim.TotalPixels > 0 ? (float)_picture.Popped / _sim.TotalPixels : 1f);
        }

        private void Step()
        {
            _events.Clear();
            _sim.Tick(_events);
            for (int i = 0; i < _events.Count; i++) Handle(_events[i]);
        }

        private void UpdateRiders(float alpha)
        {
            foreach (var rider in _sim.Riders)
            {
                if (!_views.TryGetValue(rider.Id, out var view)) continue;
                Vector2 pos = Vector2.Lerp(_layout.Node(rider.Node), _layout.Node(rider.Node + 1), alpha);
                Vector2 inward = Vector2.Lerp(_layout.Inward(rider.Node), _layout.Inward(rider.Node + 1), alpha);

                if (_boarding.TryGetValue(rider.Id, out var boarding))
                {
                    float t = (Time.time - boarding.time) / 0.16f;
                    if (t >= 1f) _boarding.Remove(rider.Id);
                    else pos = Vector2.Lerp(boarding.from, pos, Easing.OutQuad(t));
                }

                view.SetPosition(pos);
                view.SetFacing(inward);
            }
        }

        // ------------------------------------------------------------------ input

        private void HandleInput()
        {
            PointerInput.GetPresses(_presses);
            foreach (var screen in _presses)
            {
                Vector2 world = _rig.Camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, 10f));
                var shooter = FindShooter(world);
                if (shooter == null) continue;
                TryLaunch(shooter);
            }
        }

        /// <summary>The waiting shooter (lane or slot) nearest to the touch, within reach.</summary>
        private Shooter FindShooter(Vector2 world)
        {
            Shooter best = null;
            float bestDistance = _theme.ShooterSize * 0.75f;
            foreach (var shooter in _sim.Shooters)
            {
                if (shooter.State != ShooterState.Queued && shooter.State != ShooterState.Slotted) continue;
                if (!_views.TryGetValue(shooter.Id, out var view) || !view.gameObject.activeSelf) continue;
                float d = Vector2.Distance(world, view.transform.position);
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = shooter;
                }
            }
            return best;
        }

        private void TryLaunch(Shooter shooter)
        {
            var view = _views[shooter.Id];
            _events.Clear();
            var result = _sim.Launch(shooter.Id, _events);
            switch (result)
            {
                case LaunchResult.Ok:
                    for (int i = 0; i < _events.Count; i++) Handle(_events[i]);
                    break;
                case LaunchResult.BeltFull:
                    view.Deny();
                    Sfx.Play(SfxId.Warning, 1f, 0.7f);
                    Haptics.Play(HapticType.Medium);
                    if (_beltLoad != null) StartCoroutine(Tween.Punch(_beltLoad.transform, 0.3f, 0.3f, Vector3.one));
                    break;
                default:
                    view.Deny();
                    Sfx.Play(SfxId.Bump, 1.3f, 0.6f);
                    break;
            }
        }

        private void OnSlotBooster()
        {
            if (!_playing || _sim.State != SimState.Playing || _boostersLeft <= 0) return;
            _boostersLeft--;
            _events.Clear();
            _sim.AddSlot(_events);
            for (int i = 0; i < _events.Count; i++) Handle(_events[i]);
            Sfx.Play(SfxId.Hint);
            Haptics.Play(HapticType.Light);
        }

        // ------------------------------------------------------------------ events

        private void Handle(SimEvent e)
        {
            switch (e.Type)
            {
                case SimEventType.Launched: OnLaunched(e); break;
                case SimEventType.Admitted: OnAdmitted(e); break;
                case SimEventType.Shot: OnShot(e); break;
                case SimEventType.Spent: OnSpent(e); break;
                case SimEventType.Slotted: OnSlotted(e); break;
                case SimEventType.Overflow: OnOverflow(e); break;
                case SimEventType.Won: _flow = StartCoroutine(WinRoutine()); break;
                case SimEventType.SlotAdded: OnSlotAdded(); break;
            }
            RefreshHud();
        }

        private void OnLaunched(SimEvent e)
        {
            var shooter = _sim.Shooters[e.ShooterId];
            var view = _views[e.ShooterId];
            view.SetIdle(false);
            view.SetLayer(20);
            view.Puff();
            int waiting = Mathf.Max(0, _sim.Pending.Count - 1);
            view.JumpTo(_layout.Waiting(waiting), 0.3f, 1.4f);
            if (e.Slot < 0) ArrangeLane(shooter.Lane, true);
            else _slots.SetWarning(_sim.FreeSlots <= 1);

            _tapPrompt.Hide();
            Sfx.Play(SfxId.Launch);
            Haptics.Play(HapticType.Light);
        }

        private void OnAdmitted(SimEvent e)
        {
            var view = _views[e.ShooterId];
            _boarding[e.ShooterId] = (view.Position, Time.time);
            Sfx.Play(SfxId.Land, 1.1f, 0.7f);
            var fx = FxPool.Instance;
            if (fx != null) fx.Flash(_shooterRoot.TransformPoint(_layout.Node(0)), new Color(1f, 1f, 1f, 0.6f), 0.4f, 2.2f, 0.3f);
        }

        private void OnShot(SimEvent e)
        {
            if (!_views.TryGetValue(e.ShooterId, out var view)) return;
            var shooter = _sim.Shooters[e.ShooterId];
            Vector3 to = _picture.PixelWorld(e.X, e.Y);
            Vector3 from = view.transform.position;
            view.Recoil(to - from);
            var muzzle = FxPool.Instance;
            if (muzzle != null)
                muzzle.Flash(from + (to - from).normalized * (_theme.ShooterSize * 0.55f), new Color(1f, 1f, 0.9f, 0.9f), 0.2f, 0.75f, 0.12f, FxShape.Circle);
            view.SetAmmo(shooter.Ammo, true);
            Sfx.Play(SfxId.Shoot, 1f + Random.Range(-0.05f, 0.05f), 0.6f);

            int x = e.X, y = e.Y;
            _projectiles.Fire(from, to, _theme.ColorOf(shooter.Color), () =>
            {
                _popCombo = Time.time - _lastPopTime < 0.3f ? _popCombo + 1 : 0;
                _lastPopTime = Time.time;
                _picture.Pop(x, y, 1f + Mathf.Min(_popCombo, 14) * 0.035f, Mathf.Clamp01(_popCombo / 12f));
                if (_popCombo % 3 == 0) Haptics.Play(HapticType.Light);
            });
        }

        private void OnSpent(SimEvent e)
        {
            if (!_views.TryGetValue(e.ShooterId, out var view)) return;
            _views.Remove(e.ShooterId);
            _boarding.Remove(e.ShooterId);
            StartCoroutine(SpendAfterShot(view));
        }

        private IEnumerator SpendAfterShot(ShooterView view)
        {
            yield return new WaitForSeconds(0.12f);
            if (view == null) yield break;
            Sfx.Play(SfxId.Spent);
            yield return view.Vanish();
        }

        private void OnSlotted(SimEvent e)
        {
            var view = _views[e.ShooterId];
            _boarding.Remove(e.ShooterId);
            view.JumpTo(_layout.Slot(e.Slot, _sim.SlotCount), 0.35f, 1.1f);
            view.SetFacing(Vector2.up);
            view.SetIdle(true);
            view.SetLayer(0);
            Sfx.Play(SfxId.Slot);

            _slots.SetWarning(_sim.FreeSlots <= 1);
            if (_sim.FreeSlots == 0)
            {
                Sfx.Play(SfxId.Warning);
                Haptics.Play(HapticType.Medium);
            }
        }

        private void OnSlotAdded()
        {
            _slots.AddSlot();
            for (int slot = 0; slot < _sim.SlotCount; slot++)
            {
                var parked = _sim.SlotAt(slot);
                if (parked != null && _views.TryGetValue(parked.Id, out var view)) view.MoveTo(_layout.Slot(slot, _sim.SlotCount), 0.25f);
            }
            _slots.SetWarning(_sim.FreeSlots <= 1);
        }

        private void OnOverflow(SimEvent e)
        {
            if (_views.TryGetValue(e.ShooterId, out var view)) view.Deny();
            _rig.Shake(0.6f);
            Haptics.Play(HapticType.Heavy);
            _flow = StartCoroutine(LoseRoutine());
        }

        private void RefreshHud()
        {
            if (_beltLoad != null) _beltLoad.text = $"Belt {_sim.BeltLoad}/{_sim.BeltCapacity}";
            if (_slotBoosterCount != null) _slotBoosterCount.text = _boostersLeft.ToString();
            if (_slotBooster != null) _slotBooster.interactable = _boostersLeft > 0;
        }

        // ------------------------------------------------------------------ end of level

        private IEnumerator WinRoutine()
        {
            _playing = false;
            _topBar.HideTip();
            while (_projectiles.InFlight > 0) yield return null;
            yield return new WaitForSeconds(0.25f);

            _belt.Running = false;
            _rig.Shake(0.25f);
            Sfx.Play(SfxId.Win);
            Haptics.Play(HapticType.Success);
            _rig.ZoomPunch(0.05f);
            StartCoroutine(_picture.PlayWinSweep());
            var fx = FxPool.Instance;
            if (fx != null)
            {
                float half = _layout.HalfBeltX;
                fx.ConfettiBurst(new Vector2(-half, -_layout.HalfBeltY), 50, _layout.HalfBeltY * 3.2f, 0.3f, _theme.Confetti);
                fx.ConfettiBurst(new Vector2(half, -_layout.HalfBeltY), 50, _layout.HalfBeltY * 3.2f, 0.3f, _theme.Confetti);
            }

            int count = PixelLevels.All.Count;
            bool last = _levelIndex == count - 1;
            int next = Progress.CompleteLevel(GameId, _levelIndex, count);
            yield return new WaitForSeconds(0.6f);

            if (last) _winPanel.Show("All Clear!", "Every picture is popped.\nPlay them again?", "Play Again", () => LoadLevel(next));
            else _winPanel.Show("Picture Popped!", $"{PixelLevels.All[_levelIndex].Name} is done.", "Next", () => LoadLevel(next));
            _flow = null;
        }

        private IEnumerator LoseRoutine()
        {
            _playing = false;
            _belt.Running = false;
            yield return new WaitForSeconds(0.7f);
            Sfx.Play(SfxId.Fail);
            Haptics.Play(HapticType.Failure);
            _losePanel.Show("No Room Left!", "A shooter came back with ammo\nbut every slot was full.", "Try Again", () => LoadLevel(_levelIndex));
            _flow = null;
        }
    }
}
