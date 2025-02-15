/// <summary>
/// EmmySharp, created by Dylan Rafael (floofer++) for use by 0thElement.
/// The below classes help to generate EmmyLua workspace files from MoonSharp classes.
/// </summary>

using System;

namespace EmmySharp
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class EmmyTypeAttribute : Attribute
    {
        public EmmyTypeAttribute(Type type)
        {
            Type = type;
        }

        public EmmyTypeAttribute(string raw)
        {
            Raw = raw;
        }

        public Type Type { get; }

        public string Raw { get; }
    }
}