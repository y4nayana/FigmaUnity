using DA_Assets.FCU.Model;

namespace DA_Assets.FCU
{
    public static class PrefabCreatorExtensions
    {
        public static bool CanBePrefab(this SyncData sd)
        {
            bool onlyCont = sd.Tags.Contains(FcuTag.Container) && sd.Tags.Count == 1;
            return !onlyCont;
        }
    }
}
