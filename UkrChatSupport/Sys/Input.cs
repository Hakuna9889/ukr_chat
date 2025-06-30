using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Keys;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;

namespace UkrChatSupport.Sys;

public unsafe class Input
{
    public static bool IsGameFocused => !Framework.Instance()->WindowInactive;
    public static bool IsGameTextInputActive => RaptureAtkModule.Instance()->AtkModule.IsTextInputActive();
    public static bool Disabled => IsGameTextInputActive || !IsGameFocused || ImGui.GetIO().WantCaptureKeyboard;

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetKeyboardState(byte[] lpKeyState);
    private static readonly byte[] keyboardState = new byte[256];

    public void Update()
    {
        GetKeyboardState(keyboardState);
    }

    public bool IsDown(VirtualKey key) => (keyboardState[(int)key] & 0x80) != 0;
}
