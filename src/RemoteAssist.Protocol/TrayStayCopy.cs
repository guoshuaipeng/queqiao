using System;

namespace RemoteAssist.Protocol;

/// <summary>
/// Windows 接受协助端：关窗留在托盘的说明只留一处（R032）。
/// 启动气泡保留；底栏默认不写常驻托盘句；关窗藏托盘后可一次性反馈。
/// </summary>
public static class TrayStayCopy
{
    /// <summary>启动托盘气泡（无返回首页）。</summary>
    public const string BalloonDefault =
        "已在托盘运行。关闭主窗口不会退出，可随时双击托盘图标打开。";

    /// <summary>启动托盘气泡（有返回首页）。</summary>
    public const string BalloonWithReturnHome =
        "被控模式已启动。可返回首页切换为控制端；关闭主窗口会留在托盘。";

    /// <summary>用户点关闭、窗口藏到托盘后的底栏一次性反馈（不写进默认底栏）。</summary>
    public const string HiddenToTrayFeedback =
        "已隐藏到托盘。双击托盘图标可重新打开。";

    /// <summary>曾在底栏默认出现、本轮去掉的常驻重复句（回归用，勿再当作默认底栏）。</summary>
    public const string RemovedDefaultStatusBar =
        "就绪。关闭窗口后会继续在托盘运行。";

    public static string BalloonFor(bool enableReturnHome) =>
        enableReturnHome ? BalloonWithReturnHome : BalloonDefault;
}
