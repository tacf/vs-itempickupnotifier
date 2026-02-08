
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace ItemPickupNotifier.GUI
{
    static class UITils
    {
        static public ElementBounds Under(ElementBounds x) => x.FlatCopy().FixedUnder(x);
        public static string GetLangString(string modkey, string key)
        {
            return Lang.Get(modkey + ":"+ key);
        }
    }
}