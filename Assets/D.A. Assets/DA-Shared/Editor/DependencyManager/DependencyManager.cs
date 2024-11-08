using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Callbacks;

namespace DA_Assets.DM
{
    public class DefineModifier
    {
        [DidReloadScripts]
        private static void OnScriptsReload()
        {
            _ = SearchAssets();
        }

        public static void Apply()
        {
            List<string> enabled = new List<string>();
            List<string> disabled = new List<string>();

            foreach (DependencyItem assemblyConfig in DependencyItems.Instance.Items)
            {
                if (assemblyConfig.Enabled)
                {
                    enabled.Add(assemblyConfig.ScriptingDefineName);
                }
                else
                {
                    disabled.Add(assemblyConfig.ScriptingDefineName);
                }
            }

            //enabled.Add("FCU_EXISTS");

            Modify(enabled.ToArray(), disabled.ToArray());
        }

        internal static async Task SearchAssets()
        {
          await Task.Delay(1000);
            List<DependencyItem> l = DependencyItems.Instance.Items;

            for (int i = 0; i < l.Count; i++)
            {
                Type t = Type.GetType(l[i].Type);

                if (t == null)
                    continue;

                DependencyItem ac = l[i];

                //Ignoring Unity.Plastic.Newtonsoft.Json.dll
                if (t.Assembly.Location.EndsWith(Path.Combine("Editor", "Data", "Managed", "Managed", "Newtonsoft.Json.dll")))
                    continue;

                ac.Enabled = true;
                l[i] = ac;

                await Task.Yield();
            }

            Apply();
        }

        internal static void Modify(string[] addDefines, string[] removeDefines)
        {
            List<string> finalDefines = GetDefines();

            finalDefines.AddRange(addDefines);
            finalDefines.RemoveAll(x => removeDefines.Contains(x));
            finalDefines = finalDefines.Distinct().ToList();

            SetDefines(finalDefines);
        }

        internal static List<string> GetDefines()
        {
            string rawDefs;

#if UNITY_2022_3_OR_NEWER
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            rawDefs = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            rawDefs = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif

            if (string.IsNullOrWhiteSpace(rawDefs) == false)
            {
                return rawDefs.Split(';').ToList();
            }

            return new List<string>();
        }

        internal static void SetDefines(List<string> defines)
        {
            string joinedDefs = string.Join(";", defines);

#if UNITY_2022_3_OR_NEWER
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, joinedDefs);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, joinedDefs);
#endif
        }
    }
}
