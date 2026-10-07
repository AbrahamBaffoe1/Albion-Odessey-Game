# Stockwell source assessment

A newly located 1939 architectural reference supplies Stockwell ground, first and second-floor plans plus a transverse section showing six stack tiers. See `Art/Architecture/Stockwell-historical-evidence.json` for provenance, page mapping and reviewed measurements. The scanned book has inconsistent front-matter/page offsets: Albion is on PDF pages 83–84, printed pages 84–85. Pages 87–88 concern Bluffton, not Albion.

The historical width is labeled 120 feet (36.576 m), compared with the current generic model width of 24 m. Reading-room ceilings are described as 15 feet (4.572 m); this must not be assigned blindly as a floor-to-floor spacing. The source's outer 156/138-foot dimension lines need interpretation.

The current campus model cannot yet claim Stockwell fidelity. Before replacing it, reconcile the historical shell and section with modern photos and the documented 1980 Mudd connection and 2011 main-floor renovation. Historical room functions are not automatically current functions. No live geometry was changed during this source assessment.

## Current configuration and original construction archive

The official library guide still lists all six Stockwell tiers, including group study on Tier 5, alongside lower/main/upper building levels. This establishes a real missing feature in the generic three-floor game model. The guide is schematic and does not establish surveyed vertical offsets or stair dimensions. See `Stockwell-current-topology.json`.

The University of Pennsylvania Day & Klauder Collection, holding 069.4, lists 14 original construction drawings. Seven public gallery records are linked from the holding page. The inspected record includes elevations, sections and plans dated 20 February 1937. Its 250 × 180 public preview is not readable enough for dimensions; high-resolution online viewing requires a PAB subscriber login. See `Stockwell-drawing-archive.json`. No dimension was inferred from that thumbnail.

## Currently published exterior photograph

The college's Current Students page links `FJG-ALBION-0227-687x458.jpg`, a clear view of the central façade and approach. It confirms the pale two-story pilasters, five central upper window bays, named entablature, pedimented portico, raised terrace, stair sidewalls/coping and railings. The image's capture date is not established. It does not measure elevations, step sizes, total width or hidden roof/connector geometry.

The generic game façade lacks these features. Correct its entrance datum and internal floor elevations together; adding a decorative elevated stair while retaining the current ground-level entrance would create a false reconstruction. Evidence is recorded in `Stockwell-current-elevation.json`.

## Isolated façade study

`StockwellFacadeStudy.cs` provides a separate review scene through `-stockwellFacadeStudy`. It uses the historical 36.576 m width and the photographed five-bay pilaster arrangement, entablature, portico, raised terrace and stair coping. Height, opening sizes, stair rise/run and most proportions remain estimates. Brick UVs use consistent model-space scale rather than stretching over the entire wall.

The actual player controller climbed the provisional entrance steps to the terrace successfully. This checks only that staged route; it does not verify a working entrance, interior floors, complete collisions or historical stair accuracy. The study has no verified current roof, detailed rear elevation, Mudd connection or interior and is not installed in the live campus. It remains visibly simplified and is not an exact or photorealistic replica.

A second photograph comparison corrected the study's cylindrical portico supports to square posts with bases/capitals and replaced the sash-like entry with a glazed door/transom/sidelight assembly. It also adds a simplified pediment radial motif, lantern standards and crossed terrace railing braces. These features are visible in the source; their dimensions, counts outside the crop and ornamental profiles remain provisional. Door leaves are not operable.

## Scaled shell trace

`Tools/Architecture/trace_stockwell_shell.py` records manually reviewed pixel landmarks on the 1741 × 2400 rendering of printed page 85. Calibrating the first-floor outer width to the labeled 120 feet gives an approximate 18.6841 m central façade span and 19.5754 m shell depth. The façade study now uses the resulting approximately 3.7368 m five-bay pitch instead of its earlier unsupported 4.5 m pitch. Terrace parapet ends and side railing starts were adjusted to meet the narrower central section.

These decimal values are reproducible calculations, not precision claims. Landmark reading uncertainty is approximately six pixels before unknown scan distortion; the plan does not establish current pilaster centre lines. Depth additionally assumes equal horizontal and vertical scan scale. No historical room was installed as a current room. See `Art/Architecture/Stockwell-shell-trace.json` for coordinates and limitations.

## Historical shell volume

The study now includes the approximately 19.5754 m plan-scaled depth, side/rear wall masses and a hipped roof. The historical exterior photograph on printed page 84 confirms the hip form; the transverse section on page 85 suggests an approximate 0.25 roof rise/span ratio. Roof finish, eave projection and current configuration are provisional. The two historical roof stacks were recorded as evidence but not reproduced without current confirmation.

This combines a historically supported shell with current photographed entrance features for reconstruction review. It is neither a complete historical model nor a verified current replica. Blank side/rear masses indicate unresolved openings, not evidence that those walls lack windows. All interior geometry, current connector interfaces and elevation reconciliation remain outstanding.

## Archives history clarification

Albion College Archives & Special Collections published a library history in 2022: https://storymaps.arcgis.com/stories/06d8caba4d5c4b139fc7340d7d3e54a4 . It distinguishes the 2011 reopening of Stockwell's front entrance/cafe changes from Cutler Center occupancy on first and second floors in 2020. Older descriptions of a sealed Stockwell entrance must not control today's game route.

Its Mudd bridge illustration was downloaded and visually reviewed. It supports separate building masses joined by a lower enclosed connection, but is explicitly an architectural rendering rather than an as-built record. It does not resolve present connector dimensions or floor interfaces. Current side/rear photographs remain missing. Provenance and implementation consequences are saved in `Stockwell-archives-history.json`.
