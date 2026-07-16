#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace DeadWalls
{
    [DefaultExecutionOrder(1000)]
    public sealed class DevelopmentTestPanel : MonoBehaviour
    {
        private const int WindowId = 0x445754;
        private Rect _windowRect;
        private bool _expanded = true;
        private string _status = "Ready. Start with UNLOCK TEST COMBAT.";
        private Texture2D _windowTexture;
        private GUIStyle _windowStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _smallStyle;
        private GUIStyle _statusStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<DevelopmentTestPanel>() != null)
                return;

            var panelObject = new GameObject("[DeadWalls] Development Test Panel")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            DontDestroyOnLoad(panelObject);
            panelObject.AddComponent<DevelopmentTestPanel>();
        }

        private void OnDestroy()
        {
            if (_windowTexture != null)
                Destroy(_windowTexture);
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (!_expanded)
            {
                Rect collapsedRect = new Rect(Mathf.Max(12f, Screen.width - 154f), 58f, 142f, 34f);
                GUI.backgroundColor = new Color(0.95f, 0.62f, 0.18f);
                if (GUI.Button(collapsedRect, "DEV TESTS"))
                    _expanded = true;
                GUI.backgroundColor = Color.white;
                return;
            }

            _windowRect = new Rect(Mathf.Max(12f, Screen.width - 354f), 58f, 342f, 282f);
            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, GUIContent.none, _windowStyle);
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("DEVELOPMENT TESTS", _titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("—", GUILayout.Width(30f), GUILayout.Height(24f)))
                _expanded = false;
            GUILayout.EndHorizontal();

            GUILayout.Label("PLAY MODE ONLY  •  ACTIVATION BLOCKS RUN SAVE", _smallStyle);
            GUILayout.Space(6f);

            GUI.backgroundColor = new Color(0.96f, 0.62f, 0.16f);
            if (GUILayout.Button("UNLOCK TEST COMBAT + FREE BUY", GUILayout.Height(34f)))
                EnableCombat();

            GUI.backgroundColor = new Color(0.22f, 0.70f, 0.46f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("SPAWN 2K", GUILayout.Height(36f)))
                Spawn(DevelopmentTestRules.Horde2K);
            if (GUILayout.Button("SPAWN 5K", GUILayout.Height(36f)))
                Spawn(DevelopmentTestRules.Horde5K);
            if (GUILayout.Button("SPAWN 10K", GUILayout.Height(36f)))
                Spawn(DevelopmentTestRules.Horde10K);
            GUILayout.EndHorizontal();

            GUI.backgroundColor = new Color(0.30f, 0.53f, 0.84f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("RESET COOLDOWNS", GUILayout.Height(30f)))
                ResetCooldowns();
            if (GUILayout.Button("CLEAR HORDE", GUILayout.Height(30f)))
                ClearHorde();
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            GUILayout.Space(7f);
            GUILayout.Label(_status, _statusStyle, GUILayout.MinHeight(46f));
            GUILayout.Label("Stop Play Mode to restore the untouched run.", _smallStyle);
            GUI.DragWindow(new Rect(0f, 0f, 300f, 30f));
        }

        private void EnableCombat()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                _status = "GameManager is not ready yet.";
                return;
            }

            gameManager.TryEnableDevelopmentCombat(out _status);
        }

        private void ResetCooldowns()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                _status = "GameManager is not ready yet.";
                return;
            }

            gameManager.TryResetDevelopmentCooldowns(out _status);
        }

        private void Spawn(int count)
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                _status = "GameManager is not ready yet.";
                return;
            }

            gameManager.TrySpawnDevelopmentHorde(count, out _, out _status);
        }

        private void ClearHorde()
        {
            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                _status = "GameManager is not ready yet.";
                return;
            }

            int returned = gameManager.ClearDevelopmentHorde();
            _status = $"Returned {returned:N0} zombies to the pool.";
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null)
                return;

            _windowTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _windowTexture.SetPixel(0, 0, new Color(0.035f, 0.045f, 0.065f, 0.97f));
            _windowTexture.Apply(false, true);

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(14, 14, 12, 12)
            };
            _windowStyle.normal.background = _windowTexture;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.76f, 0.34f) }
            };
            _smallStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(0.60f, 0.66f, 0.74f) }
            };
            _statusStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                wordWrap = true,
                padding = new RectOffset(9, 9, 6, 6),
                normal = { textColor = new Color(0.88f, 0.91f, 0.95f) }
            };
        }
    }
}
#endif
