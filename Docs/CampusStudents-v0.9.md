# Campus student population · v0.9

The exterior Albion College map now has **16 named student route agents**. They circulate between the Quad and arrival plaza, Legacy Hall, Robinson and Mudd learning spaces, the Science Complex, Baldwin Dining Commons, Kellogg Commons, residence streets, Greek-life houses, the athletic complex, Whitehouse Nature Center and the equestrian district. Activity agents add students who study, eat, work, dance and play at the authored stations inside those destinations.

The fictional roster is intentionally mixed. It includes women, men, trans women, trans men, non-binary, genderfluid and agender students, with varied pronouns, skin tones, hair, clothing and backpack choices. The same profile data is used for the classroom roster, where a full 12-seat class keeps the visual mix instead of falling back to identical placeholder students.

Each visible agent has a `CampusStudentIdentity` component with a name, gender identity and pronouns. This is local game metadata for future dialogue, accessibility and multiplayer systems; it is not a gameplay stat and it does not represent a real student. The offline chapter does not publish personal information or infer identity from appearance.

The population is deterministic so screenshots, smoke tests and classroom walkthroughs remain reproducible. Agents use direct authored routes and do not add collision to the campus; squirrels treat them as moving scenery and continue to roam their tree habitat.
