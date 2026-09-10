"""Validate the OpenXR package, loader and runtime hooks are present."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
manifest = json.loads((root / "Unity/Packages/manifest.json").read_text())
deps = manifest["dependencies"]
required = {
    "com.unity.inputsystem": "1.20.0",
    "com.unity.xr.management": "4.7.0",
    "com.unity.xr.openxr": "1.18.0",
    "com.unity.xr.interaction.toolkit": "3.6.0",
    "com.unity.xr.hands": "1.9.0",
    "com.unity.xr.oculus": "4.5.5",
}
for package, version in required.items():
    assert deps.get(package) == version, f"{package} is not pinned to {version}"

settings = (root / "Unity/Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset").read_text()
assert "buildTarget: 1" in settings and "buildTarget: 7" in settings
assert settings.count("m_AutomaticLoading: 1") >= 2
assert settings.count("m_AutomaticRunning: 1") >= 2
loader = root / "Unity/Assets/XR/Loaders/OpenXRLoader.asset"
assert loader.exists() and loader.stat().st_size > 100
runtime = (root / "Unity/Assets/Scripts/OdysseyXRExperience.cs").read_text()
for marker in ("CommonUsages.primary2DAxis", "CommonUsages.triggerButton", "CommonUsages.gripButton", "XRHandSubsystem", "ApplyRoomScale", "ODYSSEY_XR_RECOVERED"):
    assert marker in runtime, f"missing XR runtime hook: {marker}"
print("PASS: OpenXR packages, Standalone/Android loaders, controller input, hand tracking, room-scale movement and recovery hooks")
