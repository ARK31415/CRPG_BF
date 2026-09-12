/// <summary>
/// 玩家请求“全局取消 / 返回”（Esc 键触发）。
/// 不等同于“请求暂停”；最终语义由 BF_EscapeRouter 按当前上下文裁决。
/// 唯一发布者：BF_InputManager；唯一订阅者：BF_EscapeRouter；其他系统只能实现 IBF_EscapeHandler。
/// </summary>
public class BF_EscapeRequestEvent : IGameEvent
{
}
