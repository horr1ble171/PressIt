using PressIt.Models;
using WindowsInput.Native;

namespace PressIt.Services;

public class ScenarioRunner
{
    private readonly KeyboardService _keyboard;
    private CancellationTokenSource? _cts;

    public bool IsRunning => _cts is not null && !_cts.IsCancellationRequested;

    public event Action<string>? StatusChanged;
    public event Action? Finished;

    public ScenarioRunner(KeyboardService keyboard)
    {
        _keyboard = keyboard;
    }

    public async Task RunAsync(List<ScenarioAction> actions)
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        try
        {
            foreach (var action in actions)
            {
                token.ThrowIfCancellationRequested();

                if (action.DelayMs > 0)
                    await Task.Delay(action.DelayMs, token);

                token.ThrowIfCancellationRequested();

                var key = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), action.Key);

                switch (action.ActionType)
                {
                    case ActionType.Press:
                        _keyboard.HoldKey(key);
                        StatusChanged?.Invoke($"Зажата {action.Key}");
                        break;
                    case ActionType.Release:
                        _keyboard.ReleaseKey(key);
                        StatusChanged?.Invoke($"Отпущена {action.Key}");
                        break;
                    case ActionType.Tap:
                        _keyboard.TapKey(key);
                        StatusChanged?.Invoke($"Нажата {action.Key}");
                        break;
                }
            }

            StatusChanged?.Invoke("Сценарий завершён");
        }
        catch (OperationCanceledException)
        {
            _keyboard.ReleaseAll();
            StatusChanged?.Invoke("Сценарий остановлен");
        }
        catch (Exception ex)
        {
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
    }
}
