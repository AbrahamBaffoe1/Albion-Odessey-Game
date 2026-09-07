using System;
using System.Collections.Generic;
using UnityEngine;
namespace AlbionOdyssey
{
    public sealed class OdysseyAudio : MonoBehaviour
    {
        readonly OdysseyFeedback feedback=new OdysseyFeedback();
        readonly Dictionary<OdysseyCue,AudioClip> clips=new Dictionary<OdysseyCue,AudioClip>();
        readonly Queue<OdysseyCue> achievements=new Queue<OdysseyCue>();
        AudioSource actions,celebration;
        float nextAchievement,prefsDue=-1;
        public float Volume {get;private set;}=.65f;
        public bool Muted {get;private set;}
        public string AchievementCaption {get;private set;}="";
        float captionUntil;
        public int PlayedCount {get;private set;}
        public int LoadedCount=>clips.Count;
        public int PendingAchievements=>achievements.Count;
        public void Initialize(OdysseyState state)
        {
            foreach(OdysseyCue cue in Enum.GetValues(typeof(OdysseyCue)))
            {
                var clip=Resources.Load<AudioClip>("Audio/"+cue);
                if(clip==null)throw new InvalidOperationException("Missing sound cue: "+cue);
                clips.Add(cue,clip);
            }
            actions=gameObject.AddComponent<AudioSource>();celebration=gameObject.AddComponent<AudioSource>();
            foreach(var source in new[]{actions,celebration}){source.playOnAwake=false;source.loop=false;source.spatialBlend=0;source.pitch=1;source.priority=32;}
            if(!OdysseySmoke.Enabled){Volume=Mathf.Clamp01(PlayerPrefs.GetFloat("Odyssey.EffectsVolume",.65f));Muted=PlayerPrefs.GetInt("Odyssey.EffectsMuted",0)!=0;}
            else Muted=true; // The accelerated walkthrough would stack many rewards in a single frame.
            ApplyVolume();feedback.Reset(state);
        }
        public void Observe(OdysseyState state)
        {
            foreach(var cue in feedback.Observe(state))Play(cue);
        }
        public AudioClip Clip(OdysseyCue cue)=>clips[cue];
        public void ResetBaseline(OdysseyState state)
        {feedback.Reset(state);achievements.Clear();actions.Stop();celebration.Stop();AchievementCaption="";nextAchievement=0;}
        public void Play(OdysseyCue cue)
        {
            if(OdysseyFeedback.IsAchievement(cue))
            {
                if(achievements.Count==0&&!celebration.isPlaying)nextAchievement=Time.unscaledTime+.28f;
                achievements.Enqueue(cue);
            }
            else {actions.PlayOneShot(clips[cue],.65f);PlayedCount++;}
        }
        void Update()
        {
            if(achievements.Count>0&&Time.unscaledTime>=nextAchievement)
            {
                var cue=achievements.Dequeue();celebration.clip=clips[cue];celebration.Play();PlayedCount++;
                AchievementCaption=OdysseyFeedback.Caption(cue);captionUntil=Time.unscaledTime+3;
                nextAchievement=Time.unscaledTime+clips[cue].length+.15f;
            }
            if(Time.unscaledTime>captionUntil)AchievementCaption="";
            if(prefsDue>=0&&Time.unscaledTime>=prefsDue){PlayerPrefs.Save();prefsDue=-1;}
        }
        void ApplyVolume()
        {if(actions!=null){actions.volume=Muted?0:Volume;celebration.volume=Muted?0:Volume;}}
        public void SetPreferences(float volume,bool muted)
        {
            volume=Mathf.Clamp01(volume);if(Mathf.Approximately(volume,Volume)&&muted==Muted)return;
            Volume=volume;Muted=muted;ApplyVolume();
            if(!OdysseySmoke.Enabled){PlayerPrefs.SetFloat("Odyssey.EffectsVolume",Volume);PlayerPrefs.SetInt("Odyssey.EffectsMuted",Muted?1:0);prefsDue=Time.unscaledTime+1;}
        }
        void OnApplicationQuit(){if(!OdysseySmoke.Enabled)PlayerPrefs.Save();}
    }
}
