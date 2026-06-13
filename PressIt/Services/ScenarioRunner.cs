using PressIt;
using PressIt.Models;
using WindowsInput.Native;

namespace PressIt.Services;

public class ScenarioRunner
{
    private readonly KeyboardService _keyboard;
    private CancellationTokenSource? _cts;
    private System.Threading.Timer? _holdTimer;
    private readonly HashSet<VirtualKeyCode> _heldKeys = [];
    private readonly object _holdLock = new();

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    public event Action<string>? StatusChanged;
    public event Action? Finished;

    public ScenarioRunner(KeyboardService keyboard)
    {
        _keyboard = keyboard;
    }

    public async Task RunAsync(List<ScenarioAction> actions, int loopCount = 1)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            var infinite = loopCount <= 0;
            var pass = 0;

            while (infinite || pass < loopCount)
            {
                pass++;
                token.ThrowIfCancellationRequested();

                foreach (var action in actions)
                {
                    token.ThrowIfCancellationRequested();

                    if (action.DelayMs > 0)
                        await Task.Delay(action.DelayMs, token);

                    token.ThrowIfCancellationRequested();

                    if (!KeyHelper.Map.TryGetValue(action.Key, out var key))
                    {
                        StatusChanged?.Invoke($"Неизвестная клавиша: {action.Key}");
                        return;
                    }

                    switch (action.ActionType)
                    {
                        case ActionType.Press:
                            _keyboard.HoldKey(key);
                            StartHoldTimer(key);
                            StatusChanged?.Invoke($"Зажата {action.Key}");
                            break;
                        case ActionType.Release:
                            StopHoldTimer(key);
                            _keyboard.ReleaseKey(key);
                            StatusChanged?.Invoke($"Отпущена {action.Key}");
                            break;
                        case ActionType.Tap:
                            _keyboard.TapKey(key);
                            StatusChanged?.Invoke($"Нажата {action.Key}");
                            break;
                    }
                }

                if (infinite)
                    StatusChanged?.Invoke($"Проход {pass} выполнен");
            }

            StopAllHolds();
            _keyboard.ReleaseAll();
            StatusChanged?.Invoke("Сценарий завершён");
        }
        catch (OperationCanceledException)
        {
            StopAllHolds();
            _keyboard.ReleaseAll();
            StatusChanged?.Invoke("Сценарий остановлен");
        }
        catch (Exception ex)
        {
            StopAllHolds();
            _keyboard.ReleaseAll();
            StatusChanged?.Invoke($"Ошибка: {ex.Message}");
        }
        finally
        {
            Finished?.Invoke();
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        StopAllHolds();
        _keyboard.ReleaseAll();
    }

    private void StartHoldTimer(VirtualKeyCode key)
    {
        lock (_holdLock)
        {
            _heldKeys.Add(key);
            if (_holdTimer == null)
                _holdTimer = new System.Threading.Timer(_ => HoldTick(), null, 30, 30);
        }
    }

    private void StopHoldTimer(VirtualKeyCode key)
    {
        lock (_holdLock)
        {
            _heldKeys.Remove(key);
            if (_heldKeys.Count == 0)
            {
                _holdTimer?.Dispose();
                _holdTimer = null;
            }
        }
    }

    private void StopAllHolds()
    {
        lock (_holdLock)
        {
            _holdTimer?.Dispose();
            _holdTimer = null;
            _heldKeys.Clear();
        }
    }

    private void HoldTick()
    {
        VirtualKeyCode[] keys;
        lock (_holdLock)
        {
            if (_heldKeys.Count == 0) return;
            keys = [.. _heldKeys];
        }
        foreach (var key in keys)
            _keyboard.PressKeyDown(key);
    }
}
