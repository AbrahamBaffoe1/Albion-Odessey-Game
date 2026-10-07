# City of Albion, Michigan — version 0.17

The game world is now Albion College **and** the city around it. A walkable downtown district sits west of the campus: the college lies east of downtown, so Erie Street runs straight from the campus street into the city. Press **F7** to travel between the campus and downtown, or simply walk along Erie Street.

All positions, sizes and facades are **game-scale approximations**, not a survey. Each landmark story below uses only statements from its cited public source.

| Landmark | In the game | Source |
|---|---|---|
| Bohm Theatre, 201 S. Superior St. | Brick theatre with a gold marquee and vertical sign. Opened Christmas Day 1929; restored by the Friends of the Bohm Theatre and reopened October 16, 2014. | [Bohm Theatre](https://bohmtheatre.org/about/) |
| Gardner House Museum | Three-story mansion with a mansard roof and porch. Built 1875 by Augustus P. Gardner; thirteen rooms. | [Historical marker](https://www.hmdb.org/m.asp?m=116148) |
| Rieger Park · Mother's Day marker | Park with paths, a bench and the marker stone. First known Mother's Day observance, May 13, 1877. | [Albion guide](https://albionmich.net/historical-marker-for-mothers-day/) |
| Victory Park | Castle-themed playground, waterfall, spring pool and formal garden beds. | [Downtown Albion](https://downtownalbion.com/the-parks-of-albion-michigan/) |
| Superior Street Commercial Historic District | Two rows of storefronts, a gateway arch and the main street. | [Wikipedia](https://en.wikipedia.org/wiki/Superior_Street_Commercial_Historic_District) |
| Kalamazoo River · Albion River Trail | A river crossed by a Superior Street bridge, with a paved trail, sign and bench on the bank. | [City of Albion](https://www.cityofalbionmi.gov/visitors/activities_and_attractions/albion_river_trail.php) |
| Riverside Cemetery | Terraced hillside with headstones and a gate. | [Albion guide](https://albionmich.net/riverside-cemetery/) |

## City life

Nine pedestrians walk Superior Street, Erie Street, the river trail and the parks. They are ordinary campus student agents, so a LAN host's city walkers are mirrored to joined players. Street lamps along Superior Street glow warm at dusk and dim again by day.

## Controls

Walk near a landmark and its name appears; press **H** to read its story and **Open source** to visit the page. **F7** teleports between the campus and downtown. The river can only be crossed on the Superior Street bridge; cars can drive into the city along Erie Street.

## Implementation

`CityCatalog.cs` holds the landmark data and is engine-free; `Tests/Catalog` validates it (unique ids, https-only sources, footprints inside the district, nearest-landmark lookup). `AlbionCity.cs` builds the district and the story panel.

## Not yet done

Interiors for the city buildings, traffic on city streets, and further landmarks (for example the Albion Historical Society and North Country Trail sections). A [Superior Street storefront row](https://en.wikipedia.org/wiki/Superior_Street_Commercial_Historic_District) here is generic: individual real businesses are not modelled.
