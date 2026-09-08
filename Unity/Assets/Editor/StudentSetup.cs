using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
namespace AlbionOdyssey.Editor
{
    public sealed class StudentImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if(!assetPath.EndsWith("CampusCraft/Student/Student.fbx"))return;
            var model=(ModelImporter)assetImporter;model.animationType=ModelImporterAnimationType.Generic;model.importAnimation=true;model.globalScale=1;model.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;model.isReadable=true;
        }
        void OnPreprocessAnimation()
        {
            if(!assetPath.EndsWith("CampusCraft/Student/Student.fbx"))return;
            var model=(ModelImporter)assetImporter;var clips=model.defaultClipAnimations;
            foreach(var clip in clips){clip.loopTime=clip.name.Contains("Loop");clip.lockRootPositionXZ=true;clip.lockRootRotation=true;clip.keepOriginalOrientation=true;}
            model.clipAnimations=clips;
        }
    }
    public static class StudentSetup
    {
        public static void Prepare()
        {
            const string path="Assets/Resources/CampusCraft/Student/Student.fbx";
            AssetDatabase.Refresh();var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(model==null)throw new Exception("Student model missing");
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
            foreach(var clip in clips)Debug.Log("STUDENT_CLIP "+clip.name+" "+clip.length);
            AnimationClip Clip(string name)=>clips.FirstOrDefault(c=>c.name.EndsWith(name))??throw new Exception("Missing student clip "+name);
            const string controllerPath="Assets/Resources/CampusCraft/StudentMovement.controller";
            var existing=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);if(existing!=null)AssetDatabase.DeleteAsset(controllerPath);
            var controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Seated",AnimatorControllerParameterType.Bool);
            var machine=controller.layers[0].stateMachine;var moving=machine.AddState("Locomotion");machine.defaultState=moving;
            var blend=new BlendTree{name="Idle walk jog run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(blend,controller);
            blend.AddChild(Clip("Idle_Loop"),0);blend.AddChild(Clip("Walk_Loop"),1.7f);blend.AddChild(Clip("Jog_Fwd_Loop"),3.8f);blend.AddChild(Clip("Sprint_Loop"),6.5f);moving.motion=blend;
            var driving=machine.AddState("Driving");driving.motion=Clip("Driving_Loop");var enter=moving.AddTransition(driving);enter.hasExitTime=false;enter.duration=.18f;enter.AddCondition(AnimatorConditionMode.If,0,"Seated");var exit=driving.AddTransition(moving);exit.hasExitTime=false;exit.duration=.18f;exit.AddCondition(AnimatorConditionMode.IfNot,0,"Seated");
            var instance=UnityEngine.Object.Instantiate(model);instance.name="Albion Student";var animator=instance.GetComponent<Animator>();if(animator==null)animator=instance.AddComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    var original=mats[i];var mat=new Material(Shader.Find("Standard")){name=original.name,color=original.color};mat.SetFloat("_Glossiness",.18f);
                    string texture=original.name.Contains("Superhero")?"T_Superhero_Male_Dark":original.name.Contains("Eyes")?"T_Eye_Brown":original.name.Contains("Hair_1")?"T_Hair_1_BaseColor":"";
                    if(texture.Length>0)mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/CampusCraft/Student/"+texture+".png");
                    string mp="Assets/Resources/CampusCraft/Student/"+original.name.Replace("/","_")+".mat";
                    var old=AssetDatabase.LoadAssetAtPath<Material>(mp);if(old!=null){EditorUtility.CopySerialized(mat,old);UnityEngine.Object.DestroyImmediate(mat);mat=old;}else AssetDatabase.CreateAsset(mat,mp);mats[i]=mat;
                }
                renderer.sharedMaterials=mats;
            }
            PrefabUtility.SaveAsPrefabAsset(instance,"Assets/Resources/CampusCraft/Student.prefab");UnityEngine.Object.DestroyImmediate(instance);AssetDatabase.SaveAssets();
            Debug.Log("STUDENT_PREFAB_OK "+clips.Length+" clips");
        }
    }
}
