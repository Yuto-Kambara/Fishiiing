using System;

[Flags]
public enum ItemTags
{
    None = 0,
    Fish = 1 << 0,
    Lure = 1 << 1,
    Bait = 1 << 2,
    Reel = 1 << 3,
    Misc = 1 << 4,

    All = ~0
}
