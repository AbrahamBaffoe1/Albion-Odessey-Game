# Sound identities — Unity 0.5

Every kind has a fixed original cue. Repeating that kind reuses the exact same WAV, pitch and playback rate. Adding a new kind requires a new enum key, motif and asset; validation rejects missing mappings or identical audio files.

| Kind | Sound identity |
|---|---|
| Every golden memory pickup | One two-note bell chime, identical for all twelve memories |
| First discovery | Three rising bell notes |
| Three buildings | Four-note wooden motif |
| Eight floors explored | Rising glass-like motif |
| All four building types | Five-note construction celebration |
| Six acorns contributed | Warm four-note phrase |
| Shared Beacon completed | Long six-note finale, played once despite four profiles receiving credit |
| History / astronomy / design lesson completion | Three distinct motifs, consistent across courses using the same lesson type |
| Garden / library / observatory / hall built | Four different short construction cues |
| Reclaim / contribution / course created / enrollment / class started / paper throw | A separate fixed cue for each kind |

There are 20 cues. Achievement sounds play in sequence with a short gap; pickup and other action sounds respond immediately. Failed actions, repeated claims, opening menus, changing Keepers and loading existing progress do not earn reward sounds. Muted achievement cues still advance their queue, and gameplay text remains available.

Press **Esc** for the effects-volume slider, mute switch and pickup preview. Preferences are saved separately from game progress. The default volume is 65%. Automated smoke tests use isolated audio preferences and do not change the player's saved sound settings.

## Source and verification

`Tools/generate_audio.py` synthesizes the original 48 kHz, 16-bit mono WAV files using authored notes, harmonics and smooth envelopes. No third-party recordings or music are included. Source assets live in `Unity/Assets/Resources/Audio`; `catalog.json` records every cue's motif, duration and fingerprint.

`Tools/validate_audio.py` checks all twenty mappings, unique waveforms, sample formats, signal levels and start/end envelopes. Engine-independent feedback tests cover repeated types, distinct building/lesson kinds, initial/load/profile silence, failed actions, repeat claims and shared-Beacon deduplication.

The packaged Unity 0.5 walkthrough loaded all twenty clips, read their samples, detected the pickup signal at the AudioListener output, verified mute silence, and played two queued achievements. The existing walking, construction, classroom, quiz and save-migration tests also passed. Sound quality on other playback devices and Unreal audio integration are not yet tested.
