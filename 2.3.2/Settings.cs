using System;
using UnityModManagerNet;

//
// 背景模式
//
public enum BackgroundMode
{
    SingleGlobal,
    PerScene,
    Slideshow
}

//
// 单个视频的配置
//
[Serializable]
public class VideoConfig
{
    // videos 文件夹下的文件名，例如：bg.mp4
    public string fileName = "";

    // 显示缩放（GL 正交坐标，1 = 全屏）
    public float scale = 1f;

    // 显示偏移（GL 正交坐标）
    public float offsetX = 0f;
    public float offsetY = 0f;
}

//
// Mod 设置
//
[Serializable]
public class Settings : UnityModManager.ModSettings
{
    // 当前模式
    public BackgroundMode mode = BackgroundMode.SingleGlobal;

    // =========================
    // 单视频 / 分场景
    // =========================
    public VideoConfig globalVideo = new VideoConfig();
    public VideoConfig mainUI = new VideoConfig();   // scnLevelSelect
    public VideoConfig clUI = new VideoConfig();     // scnCLS
    public VideoConfig dlcUI = new VideoConfig();    // scnTaroMenu0
    
    // =========================
    // Slideshow（简单切换）
    // =========================
    // 幻灯片数量
    public int slideshowCount = 1;

    // 每个视频播放时长（秒）
    public float slideDuration = 10f;
    public bool enableLog = true;

    // 幻灯片视频列表
    public VideoConfig[] slideshowVideos = new VideoConfig[1];

    // =========================
    // 保存
    // =========================
    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    // =========================
    // 安全校正（必须）
    // =========================
    public void EnsureSlideshowSize()
    {
        if (slideshowCount < 1)
            slideshowCount = 1;

        if (slideshowVideos == null || slideshowVideos.Length != slideshowCount)
        {
            VideoConfig[] newArray = new VideoConfig[slideshowCount];

            for (int i = 0; i < slideshowCount; i++)
            {
                if (slideshowVideos != null && i < slideshowVideos.Length)
                    newArray[i] = slideshowVideos[i];
                else
                    newArray[i] = new VideoConfig();
            }

            slideshowVideos = newArray;
        }
    }
}