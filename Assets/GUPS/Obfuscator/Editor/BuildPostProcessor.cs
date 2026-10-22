#if UNITY_EDITOR

// System
using System;
using System.Linq;

// Unity
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace GUPS.Obfuscator
{
    public class BuildPostProcessor : IPreprocessBuildWithReport, IFilterBuildAssemblies, IPostBuildPlayerScriptDLLs, IUnityLinkerProcessor, IPostprocessBuildWithReport
    {
        // Defines if an Obfuscation Process took place.
        private static bool hasObfuscated = false;

        public int callbackOrder
        {
            get { return int.MaxValue; }
        }

        private static GUPS.Obfuscator.Editor.Settings.Unity.Editor.EditorSettings PrepareEditorSettings()
        {
            GUPS.Obfuscator.Editor.Settings.Unity.Editor.EditorSettings var_EditorSettings = new Editor.Settings.Unity.Editor.EditorSettings();

            return var_EditorSettings;
        }

        private static GUPS.Obfuscator.Editor.Settings.Unity.Build.BuildSettings PrepareBuildSettings(BuildReport _Report)
        {
            GUPS.Obfuscator.Editor.Settings.Unity.Build.BuildSettings var_BuildSettings = new Editor.Settings.Unity.Build.BuildSettings();
            var_BuildSettings.IsDevelopmentBuild = UnityEditor.EditorUserBuildSettings.development;
            var_BuildSettings.BuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            var_BuildSettings.BuildTargetGroup = UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup;
            var_BuildSettings.UnityBuildReport = _Report;

            var var_NamedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(UnityEditor.EditorUserBuildSettings.selectedBuildTargetGroup);

            var_BuildSettings.IsIL2CPPBuild = PlayerSettings.GetScriptingBackend(var_NamedBuildTarget) == ScriptingImplementation.IL2CPP;
            var_BuildSettings.Compression = _Report.summary.options.HasFlag(BuildOptions.CompressWithLz4) ? GUPS.Editor.Settings.Unity.Build.CompressionType.Lz4 :
                           _Report.summary.options.HasFlag(BuildOptions.CompressWithLz4HC) ? GUPS.Editor.Settings.Unity.Build.CompressionType.Lz4HC :
                           GUPS.Editor.Settings.Unity.Build.CompressionType.None;
            var_BuildSettings.BuildIntoProject = (UnityEditor.EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX && UnityEditor.EditorUserBuildSettings.GetPlatformSettings("OSXUniversal", "CreateXcodeProject").Equals("true"))
                || (UnityEditor.EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows && UnityEditor.EditorUserBuildSettings.GetPlatformSettings("Standalone", "CreateSolution").Equals("true"))
                || (UnityEditor.EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64 && UnityEditor.EditorUserBuildSettings.GetPlatformSettings("Standalone", "CreateSolution").Equals("true"))
                || (UnityEditor.EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64 && UnityEditor.EditorUserBuildSettings.GetPlatformSettings("Standalone", "CreateSolution").Equals("true"));

            return var_BuildSettings;
        }

        public void OnPreprocessBuild(BuildReport _Report)
        {
            // Settings
            GUPS.Obfuscator.Editor.Settings.Unity.Editor.EditorSettings var_EditorSettings = PrepareEditorSettings();
            GUPS.Obfuscator.Editor.Settings.Unity.Build.BuildSettings var_BuildSettings = PrepareBuildSettings(_Report);

            // Init
            GUPS.Obfuscator.Editor.Obfuscator.Init();
            hasObfuscated = false;

            try
            {
                // Pre Build
                GUPS.Obfuscator.Editor.Obfuscator.Singleton.PreBuild(var_EditorSettings, var_BuildSettings);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("[OPS] Error: " + e.ToString());
            }
        }

        public string[] OnFilterAssemblies(BuildOptions _BuildOptions, string[] _Assemblies)
        {
			// Return all assemblies - Filtered with build.
			return _Assemblies;
        }

        public void OnPostBuildPlayerScriptDLLs(BuildReport _Report)
        {
            if (!hasObfuscated)
            {
                if (BuildPipeline.isBuildingPlayer && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    try
                    {
                        UnityEditor.EditorApplication.LockReloadAssemblies();

                        // Settings
                        GUPS.Obfuscator.Editor.Settings.Unity.Editor.EditorSettings var_EditorSettings = PrepareEditorSettings();
                        GUPS.Obfuscator.Editor.Settings.Unity.Build.BuildSettings var_BuildSettings = PrepareBuildSettings(_Report);

                        // Obfuscate
                        GUPS.Obfuscator.Editor.Obfuscator.Singleton.PostAssemblyBuild(var_EditorSettings, var_BuildSettings);
                        hasObfuscated = true;
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError("[OPS] Error: " + e.ToString());
                    }
                    finally
                    {
                        UnityEditor.EditorApplication.UnlockReloadAssemblies();
                    }
                }
            }
        }

        public string GenerateAdditionalLinkXmlFile(BuildReport _Report, UnityLinkerBuildPipelineData _Data)
        {
            if (hasObfuscated)
            {
                try
                {
                    // Settings
                    GUPS.Obfuscator.Editor.Settings.Unity.Editor.EditorSettings var_EditorSettings = PrepareEditorSettings();
                    GUPS.Obfuscator.Editor.Settings.Unity.Build.BuildSettings var_BuildSettings = PrepareBuildSettings(_Report);

                    // Post Build
                    GUPS.Obfuscator.Editor.Obfuscator.Singleton.PostAssetsBuild(var_EditorSettings, var_BuildSettings);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("[OPS] Error: " + e.ToString());
                }
            }

            return null;
        }

#if UNITY_2021_2_OR_NEWER
#else
        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }
#endif

        public void OnPostprocessBuild(BuildReport _Report)
        {
            if (hasObfuscated)
            {
                try
                {
                    // Settings
                    GUPS.Obfuscator.Editor.Settings.Unity.Editor.EditorSettings var_EditorSettings = PrepareEditorSettings();
                    GUPS.Obfuscator.Editor.Settings.Unity.Build.BuildSettings var_BuildSettings = PrepareBuildSettings(_Report);

                    // Post Build
                    GUPS.Obfuscator.Editor.Obfuscator.Singleton.PostBuild(var_EditorSettings, var_BuildSettings);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError("[OPS] Error: " + e.ToString());
                }
            }
        }
    }
}
#endif