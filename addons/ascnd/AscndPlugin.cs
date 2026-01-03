#if TOOLS
using Godot;

namespace Ascnd.Godot;

/// <summary>
/// Editor plugin that registers the AscndClient custom node type.
/// </summary>
[Tool]
public partial class AscndPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        // Register the AscndClient custom node type
        var script = GD.Load<Script>("res://addons/ascnd/AscndClient.cs");
        var icon = GD.Load<Texture2D>("res://addons/ascnd/ascnd_icon.svg");

        AddCustomType("AscndClient", "Node", script, icon);
    }

    public override void _ExitTree()
    {
        // Clean up the custom type when plugin is disabled
        RemoveCustomType("AscndClient");
    }
}
#endif
