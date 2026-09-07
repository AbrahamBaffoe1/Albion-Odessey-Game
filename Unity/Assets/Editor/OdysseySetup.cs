using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;

namespace AlbionOdyssey.Editor
{
    public sealed class OdysseyTextures : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.Contains("/Architecture/"))return;
            var importer=(TextureImporter)assetImporter;
            importer.wrapMode=TextureWrapMode.Repeat;
            importer.maxTextureSize=2048;
            if(assetPath.EndsWith("_Normal.png"))importer.textureType=TextureImporterType.NormalMap;
            else if(assetPath.EndsWith("_Roughness.png"))importer.sRGBTexture=false;
        }
    }
    public static class OdysseySetup
    {
        [MenuItem("Odyssey/Prepare playable scene")]
        public static void Prepare()
        {
            PlayerSettings.companyName="AlbionOdyssey";
            PlayerSettings.productName="Albion Odyssey";
            PlayerSettings.bundleVersion="0.3.0";
            PlayerSettings.defaultScreenWidth=1440;
            PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedProperties();}
            ConfigureMouse();
            Directory.CreateDirectory("Assets/Scenes");Directory.CreateDirectory("Assets/Resources");
            // Keep runtime-created Standard materials and their shader variants in player builds.
            var material=new Material(Shader.Find("Standard"));
            const string materialPath="Assets/Resources/ArchitectureShader.mat";
            if(!File.Exists(materialPath))AssetDatabase.CreateAsset(material,materialPath);else UnityEngine.Object.DestroyImmediate(material);
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            var standard=Shader.Find("Standard");bool found=false;
            for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==standard)found=true;
            if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=standard;graphics.ApplyModifiedProperties();}
            const string skyPath="Assets/Resources/ArchitectureSky.mat";
            if(!File.Exists(skyPath))
            {
                var sky=new Material(Shader.Find("Skybox/Procedural"));
                sky.SetFloat("_SunSize",.025f);sky.SetFloat("_AtmosphereThickness",.8f);sky.SetFloat("_Exposure",1.1f);
                sky.SetColor("_SkyTint",new Color(.55f,.60f,.68f));
                AssetDatabase.CreateAsset(sky,skyPath);
            }
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene,"Assets/Scenes/LegacyHall.unity");
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/LegacyHall.unity",true)};
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("ODYSSEY_SETUP_OK: scene, materials, mouse input and macOS player settings ready.");
        }
        static void ConfigureMouse()
        {
            var manager=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/InputManager.asset")[0]);
            var axes=manager.FindProperty("m_Axes");
            for(int axis=0;axis<2;axis++)
            {
                string name=axis==0?"Mouse X":"Mouse Y";SerializedProperty entry=null;
                for(int i=0;i<axes.arraySize;i++)if(axes.GetArrayElementAtIndex(i).FindPropertyRelative("m_Name").stringValue==name)entry=axes.GetArrayElementAtIndex(i);
                if(entry==null){int i=axes.arraySize;axes.InsertArrayElementAtIndex(i);entry=axes.GetArrayElementAtIndex(i);}
                entry.FindPropertyRelative("m_Name").stringValue=name;
                foreach(string field in new[]{"descriptiveName","descriptiveNegativeName","negativeButton","positiveButton","altNegativeButton","altPositiveButton"})entry.FindPropertyRelative(field).stringValue="";
                entry.FindPropertyRelative("gravity").floatValue=0;entry.FindPropertyRelative("dead").floatValue=0;
                entry.FindPropertyRelative("sensitivity").floatValue=.1f;entry.FindPropertyRelative("snap").boolValue=false;entry.FindPropertyRelative("invert").boolValue=false;
                entry.FindPropertyRelative("type").intValue=1;entry.FindPropertyRelative("axis").intValue=axis;entry.FindPropertyRelative("joyNum").intValue=0;
            }
            manager.ApplyModifiedProperties();
        }
        [MenuItem("Odyssey/Build macOS game")]
        public static void BuildMac()
        {
            Prepare();
            string output=Environment.GetEnvironmentVariable("ODYSSEY_BUILD_PATH")??"Builds/Albion Odyssey.app";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/LegacyHall.unity"},locationPathName=output,target=BuildTarget.StandaloneOSX,options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Unity build failed: "+report.summary.result);
            Debug.Log("ODYSSEY_BUILD_OK "+output);
        }
    }
}
