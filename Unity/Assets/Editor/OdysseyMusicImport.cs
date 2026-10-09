using UnityEditor;
using UnityEngine;
namespace AlbionOdyssey.Editor {
 public sealed class OdysseyMusicImport:AssetPostprocessor {
  void OnPreprocessAudio(){if(!assetPath.StartsWith("Assets/Resources/Audio/Music/"))return;var importer=(AudioImporter)assetImporter;var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;importer.defaultSampleSettings=settings;importer.forceToMono=false;importer.loadInBackground=true;}
 }
}
