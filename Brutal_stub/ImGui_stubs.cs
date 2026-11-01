// Minimal compile-time stub of the Brutal.ImGui API used by KittenRemoteControl's status window.
// Signatures mirror the real Brutal.ImGui.dll (Brutal.ImGuiApi namespace); the real assembly is
// loaded by the game at runtime. Only the members the mod actually calls are declared, and only
// ones that avoid Brutal.Numerics types (float2/float4) so this stub stays self-contained.
using System;

namespace Brutal.ImGuiApi
{
    // Real Brutal.ImGui: a struct with an implicit conversion from System.String.
    public struct ImString
    {
        private string? _value;
        public static implicit operator ImString(string value) => new ImString { _value = value };
    }

    [Flags]
    public enum ImGuiWindowFlags
    {
        None = 0,
    }

    public static class ImGui
    {
        public static bool Begin(ImString name, ImGuiWindowFlags flags) => true;
        public static void Text(ImString text) { }
        public static void Separator() { }
        public static void End() { }
    }
}
