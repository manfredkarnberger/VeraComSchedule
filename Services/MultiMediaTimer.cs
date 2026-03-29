using System;
using System.Runtime.InteropServices;

namespace PcanSqliteSender.Services;

public class MultimediaTimer : IDisposable
{
    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint timeSetEvent(uint msDelay, uint msResolution, TimerCallback handler, IntPtr userCtx, uint eventType);

    [DllImport("winmm.dll", SetLastError = true)]
    private static extern uint timeKillEvent(uint uTimerID);

    private delegate void TimerCallback(uint uTimerID, uint uMsg, IntPtr dwUser, IntPtr dw1, IntPtr dw2);

    private TimerCallback? _callback;
    private uint _timerId;
    public event Action? Tick;

    public void Start(uint intervalMs)
    {
        _callback = (id, msg, user, dw1, dw2) => Tick?.Invoke();
        _timerId = timeSetEvent(intervalMs, 0, _callback, IntPtr.Zero, 1); // 1 = TIME_PERIODIC
    }

    public void Stop()
    {
        if (_timerId != 0) timeKillEvent(_timerId);
        _timerId = 0;
    }

    public void Dispose() => Stop();
}