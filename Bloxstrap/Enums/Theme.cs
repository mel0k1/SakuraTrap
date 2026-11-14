namespace Bloxstrap.Enums
{
    public enum Theme
    {
        [EnumName(FromTranslation = "Common.SystemDefault")]
        Default,
        Light,
        Dark,
        [EnumName(StaticName = "Sakura Night")]
        SakuraNight,
        [EnumName(StaticName = "Sakura Light")]
        SakuraLight
    }
}
