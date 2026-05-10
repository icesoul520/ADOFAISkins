using UnityEngine;
using UnityModManagerNet;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Video;
using System.IO;
using TMPro;
using System;
using UnityEngine.UI; // 添加UI命名空间

public static class MainClass
{
    private static UnityModManager.ModEntry mod;
    private static Settings settings;

    private static Camera targetCam;
    private static bool hooked;

    // =========================
    // 视频背景相关
    // =========================
    private static VideoPlayer singlePlayer;
    private static RenderTexture singleRT;

    private static VideoPlayer slideshowPlayer;
    private static RenderTexture slideshowRT;
    private static int slideshowIndex = 0;
    private static float slideshowTimer = 0f;
    
    // 日志限流计时器
    private static float bgLogTimer = 0f;
    private const float BG_LOG_INTERVAL = 1f;

    private static readonly string[] MENU_SCENES =
    {
        "scnLevelSelect",
        "scnCLS",
        "scnTaroMenu0",
        "TogetherLogin",
        "TogetherLobby"
    };

    // =========================
    // 字体替换相关
    // =========================
    private static TMP_FontAsset targetFont;
    private static bool fontLoaded = false;

    // =========================
    // UI替换相关 (从UIReplacer.cs合并)
    // =========================
    private static Sprite continueSprite;
    private static Sprite settingsSprite;
    private static Sprite qqSprite;
    private static Sprite quitSprite;
    private static Sprite practiceSprite;
    private static Sprite settingsPanelBackgroundSprite;
    private static Sprite levelEventsBarBackgroundSprite;
    private static bool uiReplaced = false;
    private static bool settingsPanelReplaced = false;
    private static bool levelEventsBarReplaced = false;
    private static bool sceneLoadListenerAdded = false;

    // =========================
    // Mod 入口
    // =========================

    public static void Setup(UnityModManager.ModEntry modEntry)
    {
        mod = modEntry;
        settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

        modEntry.OnUpdate = OnUpdate;
        modEntry.OnGUI = OnGUI;
        modEntry.OnSaveGUI = OnSaveGUI;

        // 加载字体
        LoadFont(modEntry);

        // 加载UI图片 (从UIReplacer.cs添加)
        LoadUISprites();

        // 添加场景加载监听 (从UIReplacer.cs添加)
        SceneManager.sceneLoaded += OnSceneLoaded;
        sceneLoadListenerAdded = true;
        mod.Logger.Log("Scene load listener added for UI replacement");
    }

    // =========================
    // UI图片加载 (从UIReplacer.cs移植)
    // =========================
    private static void LoadUISprites()
    {
        try
        {
            mod.Logger.Log("========== UI Sprites Loading START ==========");

            // 加载 Continue 图标
            continueSprite = LoadSpriteFromFile("continue.png");

            // 加载 Settings 图标
            settingsSprite = LoadSpriteFromFile("Settings.png");

            // 加载 QQ 图标
            qqSprite = LoadSpriteFromFile("QQ.png");

            // 加载 Quit 图标
            quitSprite = LoadSpriteFromFile("Quit.png");

            practiceSprite = LoadSpriteFromFile("Practice.png");

            // 加载 Settings Panel Background 图标
            settingsPanelBackgroundSprite = LoadSpriteFromFile("settingsPanelbackground.png");

            // 加载 Level Events Bar Background 图标
            levelEventsBarBackgroundSprite = LoadSpriteFromFile("levelEventsBarbackground.png");

            mod.Logger.Log("========== UI Sprites Loading END ==========");
        }
        catch (Exception e)
        {
            mod.Logger.Error("Error loading UI sprites: " + e.ToString());
        }
    }

    private static Sprite LoadSpriteFromFile(string filename)
    {
        string path = Path.Combine(mod.Path, "uis", filename);

        if (!File.Exists(path))
        {
            mod.Logger.Error(filename + " not found");
            return null;
        }

        byte[] data = File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(data);

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        mod.Logger.Log(filename + " loaded");
        return sprite;
    }

    // =========================
    // 场景加载回调 (从UIReplacer.cs移植)
    // =========================
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 场景加载时重置UI替换标志
        ResetUIReplacementFlags();
        mod.Logger.Log("Scene loaded: " + scene.name + " - Reset UI replacement flags");
        
          // 字体替换：每次场景切换执行一次（无论是否MENU_SCENES）
    if (fontLoaded && targetFont != null)
    {
        UpdateFonts();
    }
    }

    private static void ResetUIReplacementFlags()
    {
        uiReplaced = false;
        settingsPanelReplaced = false;
        levelEventsBarReplaced = false;
    }

    // =========================
    // 字体加载
    // =========================
    private static void LoadFont(UnityModManager.ModEntry modEntry)
    {
        mod.Logger.Log("========== FontLoader START ==========");

        try
        {
            string bundlePath = Path.Combine(modEntry.Path, "font/fontbundle");

            mod.Logger.Log("Bundle path: " + bundlePath);

            if (!File.Exists(bundlePath))
            {
                mod.Logger.Error("Font bundle not found.");
                return;
            }

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle == null)
            {
                mod.Logger.Error("AssetBundle load failed.");
                return;
            }

            mod.Logger.Log("AssetBundle loaded.");

            TMP_FontAsset[] fonts = bundle.LoadAllAssets<TMP_FontAsset>();

            mod.Logger.Log("TMP fonts found: " + fonts.Length);

            if (fonts.Length == 0)
            {
                mod.Logger.Error("No TMP fonts in bundle.");
                return;
            }

            targetFont = fonts[0];
            fontLoaded = true;

            mod.Logger.Log("Target font: " + targetFont.name);
            mod.Logger.Log("FontLoader initialized.");
        }
        catch (Exception e)
        {
            mod.Logger.Error("Load error: " + e);
        }

        mod.Logger.Log("========== FontLoader END ==========");
    }

    // =========================
    // 字体替换
    // =========================
    private static void UpdateFonts()
    {
        if (!fontLoaded || targetFont == null)
            return;

        try
        {
            TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
            int replaced = 0;

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text t = texts[i];

                if (t != null && t.font != targetFont)
                {
                    t.font = targetFont;
                    replaced++;
                }
            }

            if (replaced > 0)
            {
                mod.Logger.Log("Replaced fonts: " + replaced);
            }
        }
        catch (Exception e)
        {
            mod.Logger.Error("Replace error: " + e);
        }
    }

    // =========================
    // UI替换方法 (从UIReplacer.cs移植)
    // =========================
    private static void ReplaceUIBackgrounds()
    {
        // 替换设置面板背景（独立检查，不依赖其他按钮）
        if (!settingsPanelReplaced && settingsPanelBackgroundSprite != null)
        {
            ReplaceSettingsPanelBackground();
        }

        // 替换关卡事件栏背景（独立检查，不依赖其他按钮）
        if (!levelEventsBarReplaced && levelEventsBarBackgroundSprite != null)
        {
            ReplaceLevelEventsBarBackground();
        }

        // 如果按钮已经替换过，直接返回
        if (uiReplaced) return;

        // 检查所有需要的 Sprite 是否都已加载
        if (continueSprite == null || settingsSprite == null || qqSprite == null || quitSprite == null)
        {
            return;
        }

        // 查找 PauseMenu 对象
        GameObject pauseMenu = GameObject.Find("PauseMenu(Clone)");
        if (pauseMenu == null)
        {
            return;
        }

        // 尝试不同的路径组合来查找按钮
        bool allFound = true;

        // 替换 Continue 按钮
        if (!FindAndReplaceButton(pauseMenu, "Continue", continueSprite))
            allFound = false;

        // 替换 Settings 按钮
        if (!FindAndReplaceButton(pauseMenu, "Settings", settingsSprite))
            allFound = false;

        // 替换 QQ 按钮
        if (!FindAndReplaceButton(pauseMenu, "QQ", qqSprite))
            allFound = false;

        // 替换 Quit 按钮
        if (!FindAndReplaceButton(pauseMenu, "Quit", quitSprite))
            allFound = false;

        if (!FindAndReplaceButton(pauseMenu, "Practice", practiceSprite))
            allFound = false;

        if (allFound)
        {
            uiReplaced = true;
            mod.Logger.Log("All buttons replaced successfully");
        }
    }

    private static void ReplaceSettingsPanelBackground()
    {
        try
        {
            // 按指定路径查找设置面板背景对象
            GameObject backgroundObj = GameObject.Find("levelEditorScene/settingsPanel/background");

            if (backgroundObj != null)
            {
                Image img = backgroundObj.GetComponent<Image>();
                
                if (img != null)
                {
                    img.sprite = settingsPanelBackgroundSprite;
                    settingsPanelReplaced = true;
                    mod.Logger.Log("anel background replaced successfully");
                }
                else
                {
                    mod.Logger.Warning("anel background Image component not found");
                }
            }
            else
            {
                // 如果完整路径找不到，尝试其他可能的路径
                GameObject settingsPanel = GameObject.Find("settingsPanel");
                if (settingsPanel != null)
                {
                    Transform bgTransform = settingsPanel.transform.Find("background");
                    if (bgTransform != null)
                    {
                        Image img = bgTransform.GetComponent<Image>();
                        if (img != null)
                        {
                            img.sprite = settingsPanelBackgroundSprite;
                            settingsPanelReplaced = true;
                            mod.Logger.Log("Settings panel background replaced via alternative path");
                            return;
                        }
                    }
                }
                
            }
        }
        catch (Exception e)
        {
            mod.Logger.Error("Error replacing settings panel background: " + e.ToString());
        }
    }

    private static void ReplaceLevelEventsBarBackground()
    {
        try
        {
            // 按指定路径查找关卡事件栏背景对象
            GameObject backgroundObj = GameObject.Find("levelEditorScene/levelEventsPanel/background");

            if (backgroundObj != null)
            {
                Image img = backgroundObj.GetComponent<Image>();
                
                if (img != null)
                {
                    img.sprite = levelEventsBarBackgroundSprite;
                    levelEventsBarReplaced = true;
                    mod.Logger.Log("Level events bar background replaced successfully");
                }
                else
                {
                    mod.Logger.Warning("Level events bar background Image component not found");
                }
            }
            else
            {
                // 如果完整路径找不到，尝试其他可能的路径
                GameObject bottomPanel = GameObject.Find("bottomPanel");
                if (bottomPanel != null)
                {
                    Transform levelEventsBarTransform = bottomPanel.transform.Find("levelEventsBar");
                    if (levelEventsBarTransform != null)
                    {
                        Transform bgTransform = levelEventsBarTransform.Find("background");
                        if (bgTransform != null)
                        {
                            Image img = bgTransform.GetComponent<Image>();
                            if (img != null)
                            {
                                img.sprite = levelEventsBarBackgroundSprite;
                                levelEventsBarReplaced = true;
                                mod.Logger.Log("Level events bar background replaced via alternative path");
                                return;
                            }
                        }
                    }
                }

                // 尝试直接查找 levelEventsBar 对象
                GameObject levelEventsBar = GameObject.Find("levelEventsBar");
                if (levelEventsBar != null)
                {
                    Transform bgTransform = levelEventsBar.transform.Find("background");
                    if (bgTransform != null)
                    {
                        Image img = bgTransform.GetComponent<Image>();
                        if (img != null)
                        {
                            img.sprite = levelEventsBarBackgroundSprite;
                            levelEventsBarReplaced = true;
                            mod.Logger.Log("Level events bar background replaced via direct levelEventsBar find");
                            return;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            mod.Logger.Error("Error replacing level events bar background: " + e.ToString());
        }
    }

    private static bool FindAndReplaceButton(GameObject pauseMenu, string buttonName, Sprite newSprite)
    {
        // 尝试多种可能的路径
        string[] possiblePaths = new string[]
        {
            "RDPauseMenu/PauseMenu/Buttons/" + buttonName + "/Fill",
            "PauseMenu/Buttons/" + buttonName + "/Fill",
            "Buttons/" + buttonName + "/Fill",
            buttonName + "/Fill"
        };

        GameObject buttonObj = null;

        // 遍历所有可能的路径
        foreach (string path in possiblePaths)
        {
            Transform trans = pauseMenu.transform.Find(path);
            if (trans != null)
            {
                buttonObj = trans.gameObject;
                break;
            }
        }

        // 如果还没找到，尝试直接查找整个场景
        if (buttonObj == null)
        {
            buttonObj = GameObject.Find(buttonName + "/Fill");
        }

        if (buttonObj == null)
        {
            buttonObj = GameObject.Find(buttonName + "Fill");
        }

        if (buttonObj == null)
        {
            // 尝试查找所有Image组件，通过按钮名称过滤
            Image[] allImages = GameObject.FindObjectsOfType<Image>();
            foreach (Image img in allImages)
            {
                if (img.gameObject.name == "Fill" && 
                    img.transform.parent != null && 
                    img.transform.parent.name.Contains(buttonName))
                {
                    buttonObj = img.gameObject;
                    break;
                }
            }
        }

        if (buttonObj != null)
        {
            Image img = buttonObj.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = newSprite;
                mod.Logger.Log(buttonName + " button replaced");
                return true;
            }
            else
            {
                mod.Logger.Warning(buttonName + " button Image component not found");
                return false;
            }
        }
        else
        {
            mod.Logger.Warning(buttonName + " button object not found");
            return false;
        }
    }

    // =========================
    // Update
    // =========================
    private static void OnUpdate(UnityModManager.ModEntry modEntry, float dt)
    {

        // UI替换更新 (从UIReplacer.cs添加)
        ReplaceUIBackgrounds();
        
        GameObject obj = GameObject.Find("Phase 0/News Container");

        if (obj != null && obj.activeSelf)
        {
            obj.SetActive(false);
        }

        // 背景更新
        settings.EnsureSlideshowSize();

        string scene = SceneManager.GetActiveScene().name;

        if (!IsMenuScene(scene))
        {
            if (hooked)
            {
                mod.Logger.Log(string.Format("Leaving menu scene '{0}', disabling background.", scene));
                Disable();
            }
            return;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            // 无相机时也记录日志（限流）
            bgLogTimer += dt;
            if (bgLogTimer >= BG_LOG_INTERVAL)
            {
                bgLogTimer = 0f;
                mod.Logger.Log(string.Format("No main camera found in scene '{0}'", scene));
            }
            return;
        }

        if (hooked && cam != targetCam)
        {
            mod.Logger.Log(string.Format("Camera changed from '{0}' to '{1}', restarting background.", 
                (targetCam != null ? targetCam.name : "null"), cam.name));
            Disable();
            // 将继续执行重新hooked
        }

        if (!hooked)
        {
            targetCam = cam;
            mod.Logger.Log(string.Format("Entering menu scene '{0}', initializing background mode '{1}'.", 
                scene, settings.mode));

            if (settings.mode == BackgroundMode.Slideshow)
                InitSlideshow();
            else
                StartSingleVideo(scene);

            Camera.onPreCull += OnCameraPreCull;
            hooked = true;
            mod.Logger.Log("Background system hooked to OnPreCull.");
        }

        // 背景日志限流输出
        bgLogTimer += dt;
        if (bgLogTimer >= BG_LOG_INTERVAL)
        {
            bgLogTimer = 0f;
            LogCurrentBackgroundStatus(scene);
        }

        if (settings.mode == BackgroundMode.Slideshow)
            UpdateSlideshow(dt);
    }

    // 输出当前背景状态日志（每秒一次）
    private static void LogCurrentBackgroundStatus(string scene)
    {
         if (!settings.enableLog)
        return;
        if (settings.mode == BackgroundMode.Slideshow)
        {
            if (slideshowPlayer != null && slideshowRT != null)
            {
                VideoConfig cfg = settings.slideshowVideos[slideshowIndex];
                string fileName = (cfg != null && !string.IsNullOrEmpty(cfg.fileName)) ? cfg.fileName : "null";
                mod.Logger.Log(string.Format("Slideshow active: index {0}/{1}, file '{2}', player ready: {3}", 
                    slideshowIndex, settings.slideshowCount, fileName, slideshowPlayer.isPrepared));
            }
            else
            {
                mod.Logger.Log("Slideshow active but player or RT is null.");
            }
        }
        else
        {
            VideoConfig cfg = GetCurrentVideoConfig(scene);
            string fileName = (cfg != null && !string.IsNullOrEmpty(cfg.fileName)) ? cfg.fileName : "null";
            string sourceType = (singlePlayer != null) ? "Video" : ((singleRT != null) ? "Image" : "None");
            mod.Logger.Log(string.Format("Single background active: scene '{0}', file '{1}', type: {2}", 
                scene, fileName, sourceType));
        }
    }

    // =========================
    // 单视频 / 分场景
    // =========================
    private static void StartSingleVideo(string scene)
    {
        mod.Logger.Log(string.Format("StartSingleVideo called for scene '{0}'", scene));
        CleanupSingle();

        VideoConfig cfg = GetCurrentVideoConfig(scene);
        if (cfg == null || string.IsNullOrEmpty(cfg.fileName))
        {
            mod.Logger.Log(string.Format("No video config or empty fileName for scene '{0}', skipping.", scene));
            return;
        }

        string path = GetVideoPath(cfg.fileName);
        mod.Logger.Log(string.Format("Resolved video path: {0}", path));

        if (!File.Exists(path))
        {
            mod.Logger.Log(string.Format("Video file not found: {0}", path));
            return;
        }

        if (IsImage(path))
        {
            mod.Logger.Log(string.Format("File is image, loading as texture: {0}", path));
            LoadImageBackground(path);
            return;
        }

        mod.Logger.Log(string.Format("File is video, creating VideoPlayer for: {0}", path));

        GameObject go = new GameObject("ADOFAI_MenuBG_Video");
        UnityEngine.Object.DontDestroyOnLoad(go);

        singlePlayer = go.AddComponent<VideoPlayer>();
        singlePlayer.source = VideoSource.Url;
        singlePlayer.url = path;
        singlePlayer.isLooping = true;
        singlePlayer.audioOutputMode = VideoAudioOutputMode.None;

        singleRT = new RenderTexture(1920, 1080, 0);
        singlePlayer.renderMode = VideoRenderMode.RenderTexture;
        singlePlayer.targetTexture = singleRT;

        singlePlayer.Prepare();
        singlePlayer.Play();

        mod.Logger.Log(string.Format("VideoPlayer created, preparing and playing: {0}", path));
    }

    private static void LoadImageBackground(string path)
    {
        mod.Logger.Log(string.Format("LoadImageBackground: reading file '{0}'", path));
        byte[] data = File.ReadAllBytes(path);

        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(data);
        mod.Logger.Log(string.Format("Texture loaded, size: {0}x{1}", tex.width, tex.height));

        singleRT = new RenderTexture(tex.width, tex.height, 0);
        Graphics.Blit(tex, singleRT);
        mod.Logger.Log(string.Format("Image background ready, RT created: {0}x{1}", tex.width, tex.height));
    }

    // =========================
    // Slideshow
    // =========================
    private static void InitSlideshow()
    {
        mod.Logger.Log(string.Format("InitSlideshow: count={0}, duration={1}s", settings.slideshowCount, settings.slideDuration));
        CleanupSlideshow();

        slideshowIndex = 0;
        slideshowTimer = 0f;

        VideoConfig cfg = settings.slideshowVideos[slideshowIndex];
        if (cfg == null || string.IsNullOrEmpty(cfg.fileName))
        {
            mod.Logger.Log("Slideshow init failed: first slide config is null or empty.");
            return;
        }

        string path = GetVideoPath(cfg.fileName);
        if (!File.Exists(path))
        {
            mod.Logger.Log(string.Format("Slideshow init failed: file not found '{0}'", path));
            return;
        }

        mod.Logger.Log(string.Format("Slideshow starting with slide 0: '{0}'", path));

        GameObject go = new GameObject("ADOFAI_Slideshow_Player");
        UnityEngine.Object.DontDestroyOnLoad(go);

        slideshowPlayer = go.AddComponent<VideoPlayer>();
        slideshowPlayer.source = VideoSource.Url;
        slideshowPlayer.url = path;
        slideshowPlayer.isLooping = true;
        slideshowPlayer.audioOutputMode = VideoAudioOutputMode.None;
        slideshowPlayer.playOnAwake = false;

        slideshowRT = new RenderTexture(1920, 1080, 0);
        slideshowPlayer.renderMode = VideoRenderMode.RenderTexture;
        slideshowPlayer.targetTexture = slideshowRT;

        slideshowPlayer.Prepare();
        slideshowPlayer.Play();

        mod.Logger.Log("Slideshow player created and started.");
    }

    private static void UpdateSlideshow(float dt)
    {
        if (slideshowPlayer == null)
            return;

        slideshowTimer += dt;

        if (slideshowTimer < settings.slideDuration)
            return;

        slideshowTimer = 0f;
        int oldIndex = slideshowIndex;
        slideshowIndex = (slideshowIndex + 1) % settings.slideshowCount;

        mod.Logger.Log(string.Format("Slideshow switching from slide {0} to {1}", oldIndex, slideshowIndex));

        VideoConfig cfg = settings.slideshowVideos[slideshowIndex];
        if (cfg == null || string.IsNullOrEmpty(cfg.fileName))
        {
            mod.Logger.Log(string.Format("Slideshow switch failed: config for slide {0} is null or empty.", slideshowIndex));
            return;
        }

        string path = GetVideoPath(cfg.fileName);
        if (!File.Exists(path))
        {
            mod.Logger.Log(string.Format("Slideshow switch failed: file not found '{0}'", path));
            return;
        }

        mod.Logger.Log(string.Format("Loading new slide: {0}", path));

        if (IsImage(path))
        {
            mod.Logger.Log(string.Format("Slide is image, loading texture: {0}", path));
            byte[] data = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            Graphics.Blit(tex, slideshowRT);
            mod.Logger.Log(string.Format("Image slide applied to RT, size: {0}x{1}", tex.width, tex.height));
            return;
        }

        slideshowPlayer.Stop();
        slideshowPlayer.url = path;
        slideshowPlayer.Prepare();
        slideshowPlayer.Play();

        mod.Logger.Log(string.Format("Video slide started playing: {0}", path));
    }

    // =========================
    // 渲染
    // =========================
    private static void OnCameraPreCull(Camera cam)
    {
        if (cam != targetCam)
            return;

        GL.PushMatrix();
        GL.LoadOrtho();

        if (settings.mode == BackgroundMode.Slideshow)
            DrawSlideshow();
        else
            DrawSingle();

        GL.PopMatrix();
    }

    private static void DrawSingle()
    {
        if (singleRT == null)
            return;

        VideoConfig cfg = GetCurrentVideoConfig(SceneManager.GetActiveScene().name);
        if (cfg == null)
            return;

        DrawRT(singleRT, cfg);
    }

    private static void DrawSlideshow()
    {
        if (slideshowRT == null)
            return;

        VideoConfig cfg = settings.slideshowVideos[slideshowIndex];
        if (cfg == null)
            return;

        DrawRT(slideshowRT, cfg);
    }

    private static void DrawRT(RenderTexture rt, VideoConfig cfg)
    {
        float scale = Mathf.Max(cfg.scale, 0.1f);
        float w = scale;
        float h = scale;

        float x = (1f - w) * 0.5f + cfg.offsetX;
        float y = (1f - h) * 0.5f - cfg.offsetY;

        GUI.color = Color.white;
        Graphics.DrawTexture(new Rect(x, y + h, w, -h), rt);
    }

    // =========================
    // GUI
    // =========================
    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        GUILayout.Label("Background Mode");
        settings.mode = (BackgroundMode)GUILayout.SelectionGrid(
            (int)settings.mode,
            new[] { "Single", "Per Scene", "Slideshow" },
            3
        );

        GUILayout.Space(10);

        if (settings.mode == BackgroundMode.SingleGlobal)
        {
            DrawVideoConfigGUI("Global Background", settings.globalVideo);
        }
        else if (settings.mode == BackgroundMode.PerScene)
        {
            DrawVideoConfigGUI("Level Select (scnLevelSelect)", settings.mainUI);
            DrawVideoConfigGUI("CLS (scnCLS)", settings.clUI);
            DrawVideoConfigGUI("DLC (scnTaroMenu0)", settings.dlcUI);
        }
        else if (settings.mode == BackgroundMode.Slideshow)
        {
            GUILayout.Label("Slideshow Count: " + settings.slideshowCount);
            settings.slideshowCount = (int)GUILayout.HorizontalSlider(
                settings.slideshowCount, 1, 10
            );

            GUILayout.Label("Slide Duration: " + settings.slideDuration.ToString("0.0") + "s");
            settings.slideDuration = GUILayout.HorizontalSlider(
                settings.slideDuration, 3f, 30f
            );

            settings.EnsureSlideshowSize();

            for (int i = 0; i < settings.slideshowCount; i++)
            {
                DrawVideoConfigGUI("Slide " + (i + 1), settings.slideshowVideos[i]);
            }
        }

        GUILayout.Space(20);
        
        // 字体状态
        string fontStatus = fontLoaded ? "Loaded: " + (targetFont != null ? targetFont.name : "Unknown") : "Not Loaded";
        GUILayout.Label("Font Status: " + fontStatus);

        // UI替换状态 (从UIReplacer.cs添加)
        GUILayout.Space(10);
        GUILayout.Label("UI Replacement Status:");
        // 日志开关
        GUILayout.Space(10);
        settings.enableLog = GUILayout.Toggle(settings.enableLog, "Enable Log");
        GUILayout.Label("  Buttons: " + (uiReplaced ? "Replaced" : "Pending"));
        GUILayout.Label("  Settings Panel: " + (settingsPanelReplaced ? "Replaced" : "Pending"));
        GUILayout.Label("  Level Events Bar: " + (levelEventsBarReplaced ? "Replaced" : "Pending"));
    }

    private static void DrawVideoConfigGUI(string title, VideoConfig cfg)
    {
        GUILayout.Space(8);
        GUILayout.Label(title);

        GUILayout.BeginHorizontal();
        GUILayout.Label("File", GUILayout.Width(40));
        cfg.fileName = GUILayout.TextField(cfg.fileName);
        GUILayout.EndHorizontal();

        GUILayout.Label("Scale: " + cfg.scale.ToString("0.00"));
        cfg.scale = GUILayout.HorizontalSlider(cfg.scale, 0.2f, 2f);

        GUILayout.Label("Offset X: " + cfg.offsetX.ToString("0.00"));
        cfg.offsetX = GUILayout.HorizontalSlider(cfg.offsetX, -1f, 1f);

        GUILayout.Label("Offset Y: " + cfg.offsetY.ToString("0.00"));
        cfg.offsetY = GUILayout.HorizontalSlider(cfg.offsetY, -1f, 1f);
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Save(modEntry);
    }

    // =========================
    // 工具 & 清理
    // =========================
    private static bool IsImage(string path)
    {
        string ext = Path.GetExtension(path).ToLower();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    }

    private static bool IsMenuScene(string scene)
    {
        foreach (var s in MENU_SCENES)
            if (scene == s)
                return true;
        return false;
    }

    private static string GetVideoPath(string fileName)
    {
        return Path.Combine(
            UnityModManager.modsPath,
            mod.Info.Id,
            "backgrounds",
            fileName
        );
    }

    private static VideoConfig GetCurrentVideoConfig(string scene)
    {
        switch (settings.mode)
        {
            case BackgroundMode.SingleGlobal:
                return settings.globalVideo;

            case BackgroundMode.PerScene:
                if (scene == "scnLevelSelect") return settings.mainUI;
                if (scene == "scnCLS") return settings.clUI;
                if (scene == "scnTaroMenu0") return settings.dlcUI;
                break;
        }
        return null;
    }

    private static void Disable()
    {
        mod.Logger.Log("Disabling background system, cleaning up resources.");
        Camera.onPreCull -= OnCameraPreCull;
        CleanupSingle();
        CleanupSlideshow();
        targetCam = null;
        hooked = false;
        mod.Logger.Log("Background system disabled.");
    }

    private static void CleanupSingle()
    {
        if (singlePlayer != null)
        {
            mod.Logger.Log("Cleaning up single VideoPlayer.");
            singlePlayer.Stop();
            UnityEngine.Object.Destroy(singlePlayer.gameObject);
            singlePlayer = null;
        }
        if (singleRT != null)
        {
            mod.Logger.Log("Releasing single RenderTexture.");
            singleRT.Release();
            UnityEngine.Object.Destroy(singleRT);
            singleRT = null;
        }
    }

    private static void CleanupSlideshow()
    {
        if (slideshowPlayer != null)
        {
            mod.Logger.Log("Cleaning up slideshow VideoPlayer.");
            slideshowPlayer.Stop();
            UnityEngine.Object.Destroy(slideshowPlayer.gameObject);
            slideshowPlayer = null;
        }
        if (slideshowRT != null)
        {
            mod.Logger.Log("Releasing slideshow RenderTexture.");
            slideshowRT.Release();
            UnityEngine.Object.Destroy(slideshowRT);
            slideshowRT = null;
        }
        slideshowTimer = 0f;
    }
}