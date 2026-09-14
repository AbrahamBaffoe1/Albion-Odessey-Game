# Detailed campus vehicles — version 0.18

The block-built cars have been replaced with a detailed sports coupe across all 24 parked and three drivable cars. This pass provides one high-detail model with varied paint; a diverse sedan/SUV/pickup fleet is still future work.

## Vehicle art

The project adapts [Car Concept](https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/CarConcept), © 2024 Darmstadt Graphics Group GmbH, model and textures by Eric Chadwick, under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). See [complete attribution](../Art/Vehicles/CREDITS.md). GTA's [Los Santos Tuners](https://www.rockstargames.com/newswire/article/25o1829o5o4993/los-santos-tuners-coming-july-20) was a presentation reference; the distributed model is a separately licensed asset.

The editable Blender adaptation is `Art/Vehicles/CampusCoupe.blend`. It has curved body panels, wheel arches, mirrors, door seams, tires and alloy rims, brake discs/calipers, a modeled cabin, glass and lighting surfaces. The branded plate and steering emblem were removed from the runtime adaptation. Source files and hashes are retained for reproducibility.

`Tools/prepare_vehicles.py` performs the Blender adaptation and exports the FBX. `VehicleSetup` creates the shared Unity prefab and materials. Three LODs contain 92,115 / 26,625 / 7,159 triangles. Each parked car shares its meshes and materials; paint uses per-instance material properties. A single 128-pixel reflection probe captures the campus once after startup, distributed across frames.

## Play

Walk next to one of the three drivable cars and press **E**. Use **W/S** or **up/down** to accelerate/reverse, **A/D** or **left/right** to steer, and **Space** to brake. Stop before pressing **E** to exit. The static cars inside parking stalls remain scenery and collision obstacles.

Road wheels rotate with distance, front wheels steer, the cabin steering wheel turns, calipers stay on their mounts, and the red lamp emission increases during braking. The chase camera now aims at the vehicle rather than above the driver. The existing seated driver animation is positioned inside the new body.

## Verification and limits

The `-vehicleSmoke` Mac player test checks complete fleet replacement, all three LODs, wheel pivots, physical scale, model orientation, forward and reverse travel, wheel rotation, steering, brake feedback, a collision barrier, moving-exit rejection and stationary exit. It captures actual Unity front/rear/parking/driver views. The first visual review found an FBX transform problem; the export was corrected and the test now rejects undersized geometry as well as oversized geometry.

The vehicle check and all five existing player integration suites passed. [Results and screenshots](Verification-v0.18/README.md) are retained. The Mac app passed strict ad-hoc signature verification; the Quest APK compiled successfully.

This is a vehicle art and presentation upgrade. Driving still uses the existing arcade collision-and-movement model. Suspension simulation, tire grip, damage, traffic AI, entry/exit door animations and a varied vehicle catalog are not implemented by this update. The imported model's interior is retained, but detailed driver hand/foot IK is future work. A Quest build does not replace physical-headset performance testing.
