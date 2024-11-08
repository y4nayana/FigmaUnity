using DA_Assets.Constants;
using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.DAI
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/" + "Black Inspector")]
    [ResourcePath("")]
    public class BlackInspector : CustomInspector<BlackInspector>, ICustomInspector { }
}