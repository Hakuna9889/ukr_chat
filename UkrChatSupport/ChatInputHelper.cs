using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using Lumina.Text.ReadOnly;

namespace UkrChatSupport;

public unsafe class ChatInputHelper
{
    private readonly IGameGui _gameGui;
    private readonly IPluginLog _log;
    private AtkComponentInputBase* _cachedTextNode = null;

    public ChatInputHelper(IGameGui gameGui, IPluginLog log)
    {
        _gameGui = gameGui;
        _log = log;
    }

    public void SetChatInputText(string text)
    {
        var inputBase = GetValidTextNode();

        if (inputBase == null)
        {
            _log.Error("Не вдалося знайти вкладений TextNode для поля вводу.");
            return;
        }

        var utf8Text = new Utf8String(text);

        inputBase->AtkTextNode->NodeText.SetString(utf8Text);
        inputBase->UnkText2.SetString(utf8Text);
        inputBase->UnkText1.SetString(utf8Text);
    }

    public string GetChatInputText()
    {
        var inputBase = GetValidTextNode();
        if (inputBase == null)
        {
            _log.Error("Не вдалося знайти вкладений TextNode для поля вводу.");
            return string.Empty;
        }

        var seString = new ReadOnlySeString(inputBase->AtkTextNode->NodeText);
        var plainText = seString.ExtractText();

        return plainText;
    }

    private AtkComponentInputBase* GetValidTextNode()
    {
        if (_cachedTextNode != null)
            return _cachedTextNode;

        _cachedTextNode = GetInnerTextInputNode();
        return _cachedTextNode;
    }

    private AtkComponentInputBase* GetInnerTextInputNode()
    {
        var chatLogPtr = (AddonChatLog*)_gameGui.GetAddonByName("ChatLog", 1);

        if (chatLogPtr == null)
        {
            _log.Warning("AddonChatLog не знайдено.");
            return null;
        }

        var componentNode = (AtkComponentNode*)chatLogPtr->AtkUnitBase.UldManager.NodeList[16];

        if (componentNode == null)
        {
            _log.Warning("TextInput Component Node не знайдена.");
            return null;
        }
        var inputBase = (AtkComponentInputBase*)componentNode->Component;

        if (inputBase == null)
        {
            _log.Warning("Вкладена Text Node з NodeID 16 не знайдена.");
            return null;
        }

        return inputBase;
    }
}
