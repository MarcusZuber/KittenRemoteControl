// Minimal compile-time stub of the StarMap.API attributes used by KittenRemoteControl.
// Mirrors StarMap.API 0.4.5 (namespace + type names + hierarchy). The real assembly is
// provided by the StarMap loader at runtime; StarMap resolves mod hooks by attribute type
// name, so a name-compatible stub is sufficient to build against offline.
using System;

namespace StarMap.API
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class StarMapModAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public abstract class StarMapMethodAttribute : Attribute
    {
        protected StarMapMethodAttribute() { }
    }

    public sealed class StarMapBeforeMainAttribute : StarMapMethodAttribute { }
    public sealed class StarMapImmediateLoadAttribute : StarMapMethodAttribute { }
    public sealed class StarMapAllModsLoadedAttribute : StarMapMethodAttribute { }
    public sealed class StarMapUnloadAttribute : StarMapMethodAttribute { }
    public sealed class StarMapAfterOnFrameAttribute : StarMapMethodAttribute { }
    public sealed class StarMapBeforeGuiAttribute : StarMapMethodAttribute { }
    public sealed class StarMapAfterGuiAttribute : StarMapMethodAttribute { }
}
