using UnityEditor;

namespace DA_Assets.DAI
{
    public class DAEditor<T1, T2, T4> : Editor
    where T1 : DAEditor<T1, T2, T4>
    where T2 : UnityEngine.Object
    where T4 : CustomInspector<T4>, ICustomInspector
    {
        public T2 monoBeh;
        public DAInspector gui => CustomInspector<T4>.Instance.Inspector;

        public virtual void OnShow() { }

        private void OnEnable()
        {
            monoBeh = (T2)target;
            OnShow();

            DARunner.update += Repaint;
        }

        private void OnDisable()
        {
            DARunner.update -= Repaint;
        }
    }
}
