using Dalamud.IoC;
using Dalamud.Plugin;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using System;
using UkrChatSupport.Sys;
using UkrChatSupport.Windows;
using System.Threading;

namespace UkrChatSupport;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; set; } = null!;
    [PluginService] private static IGameGui GameGui { get; set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; }


    private int currentThreadId;
    private uint foregroundThreadId;
    private IntPtr foregroundWindow;
    private bool isDisposed;
    private volatile bool isHooked;
    private KeyboardHook? keyboardHook;

    private CancellationTokenSource? stopToken;


    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("UkrChatSupport");
    private ConfigWindow ConfigWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        ConfigWindow = new ConfigWindow(this);
        WindowSystem.AddWindow(ConfigWindow);
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;


        if (Configuration.ReactOnlyToUkLayout || Configuration.ReplaceOnlyOnUkLayout) 
        { 
            InitCheckerThread();
        }

        currentThreadId = KeyboardHook.GetCurrentThreadId();
        StartHook();
    }

    private bool IsUkrainianLayout()
    {
        var currentLayout = NativeMethods.GetCurrentKeyboardLayout(foregroundThreadId);
        // "uk" - ukrainian
        return currentLayout.TwoLetterISOLanguageName.Equals("uk");
    }


    private void GetForeground()
    {
        foregroundWindow = NativeMethods.GetForegroundWindow();
        foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, nint.Zero);
        if (Input.IsGameFocused)
            StartHook();
        else
            StopHook();
    }

    private void BackgroundWorker()
    {
        try
        {
            GetForeground();
            while (stopToken?.IsCancellationRequested == false)
            {
                Task.Delay(1000, stopToken.Token).Wait();
                GetForeground();
            }
        }
        catch (TaskCanceledException) { }
        catch (AggregateException ae)
        {
            if (ae.InnerException is not TaskCanceledException) throw;
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
        }
    }

    private void InitCheckerThread()
    {
        stopToken = new CancellationTokenSource();
        var backgroundThread = new Thread(BackgroundWorker)
        {
            IsBackground = true,
            Name = "Get foreground window thread"
        };
        backgroundThread.Start();
    }



    private void StartHook()
    {
        Framework.RunOnFrameworkThread(() =>
        {
            if (isHooked || keyboardHook is not null) return;
            try
            {
                keyboardHook = new KeyboardHook(true);
                keyboardHook.KeyDown += Handle_keyboardHookOnKeyDown;
                keyboardHook.OnError += Handle_keyboardHook_OnError;
                isHooked = true;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        });
    }

    private void StopHook()
    {
        Framework.RunOnFrameworkThread(() =>
        {
            if (!isHooked || keyboardHook is null) return;
            try
            {
                keyboardHook.KeyDown -= Handle_keyboardHookOnKeyDown;
                keyboardHook.OnError -= Handle_keyboardHook_OnError;
                keyboardHook.Dispose();
                keyboardHook = null;
                isHooked = false;
            }
            catch (Exception e)
            {
                Log.Error(e, e.Message);
            }
        });
    }

    private void Handle_keyboardHookOnKeyDown(Constants.Keys key, bool shift, bool ctrl, bool alt, ref bool skipNext)
    {
        try
        {
            if (!Input.IsGameFocused ||
                !Configuration.ReplaceInput ||
                !Input.IsGameTextInputActive ||
                (Configuration.ReplaceOnlyOnUkLayout && !IsUkrainianLayout())) return;

            ReplaceInput(key, shift, ref skipNext);
        }
        catch (Exception e)
        {
            Log.Error(e, e.Message);
        }
    }

    private void Handle_keyboardHook_OnError(Exception e)
    {
        Log.Error(e, e.Message);
    }

    private static void ReplaceInput(Constants.Keys aKey, bool aIsShift, ref bool aNextSkip)
    {
        foreach (var keyReplace in Constants.ReplaceKeys.Where(keyReplace => keyReplace.Key == aKey))
        {
            aNextSkip = true;
            KeyboardHook.SendCharUnicode(aIsShift ? keyReplace.RCapitalKey : keyReplace.RKey);
            return;
        }
    }


    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
    }

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleConfigUI() => ConfigWindow.Toggle();
}
