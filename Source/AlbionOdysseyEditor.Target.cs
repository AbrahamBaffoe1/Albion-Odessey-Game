using UnrealBuildTool;
using System.Collections.Generic;
public class AlbionOdysseyEditorTarget : TargetRules
{
    public AlbionOdysseyEditorTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.V5;
        ExtraModuleNames.Add("AlbionOdyssey");
    }
}
