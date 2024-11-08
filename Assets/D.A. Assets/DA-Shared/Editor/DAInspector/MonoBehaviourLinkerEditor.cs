using System;
using UnityEngine;

namespace DA_Assets.DAI
{
    public class MonoBehaviourLinkerEditor<T1, T3, T4> : MonoBehaviourLinkerEditorBase<T1, T3>
        where T1 : ScriptableObject
        where T3 : MonoBehaviour
        where T4 : CustomInspector<T4>, ICustomInspector
    {
        public override DAInspector gui => CustomInspector<T4>.Instance.Inspector;
    }

    public abstract class MonoBehaviourLinkerEditorBase<T1, T3> : LinkerBase<T3>
        where T1 : ScriptableObject
        where T3 : MonoBehaviour
    {
        public abstract DAInspector gui { get; }

        protected T1 scriptableObject;
        public T1 ScriptableObject { get => scriptableObject; set => scriptableObject = value; }
    }

    public static class MonoBehaviourLinkerEditorExtensions
    {
        public static T2 Link<T1, T2, T3>(this T3 monoBeh, ref T2 linker, T1 scriptableObject)
            where T1 : ScriptableObject
            where T2 : MonoBehaviourLinkerEditorBase<T1, T3>
            where T3 : MonoBehaviour
        {
            bool needInit = false;

            if (linker == null)
            {
                needInit = true;
                linker = (T2)Activator.CreateInstance(typeof(T2));
            }

            if (linker.MonoBeh == null)
            {
                linker.MonoBeh = monoBeh;
            }

            if (linker.ScriptableObject == null)
            {
                linker.ScriptableObject = scriptableObject;
            }

            if (needInit)
            {
                linker.OnLink();
            }

            return linker;
        }
    }
}
