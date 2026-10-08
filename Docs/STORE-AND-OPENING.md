# Store and cinematic opening — 0.32.0

The opening transitions from original illustrated Albion key art, with stage-driven loading progress, into a separate oak-and-acorn title scene. It uses Cinzel (SIL Open Font License) for the title and the existing Rajdhani interface family for store menus. Reduced-motion mode removes drifting particles and shortens transitions. The brief introductory hold is cinematic pacing, not a fabricated download percentage.

The store is available from the title screen, Explore Albion and Pause. It offers six exclusive student sweatshirt finishes, a featured carousel, an alphabetical catalog, owned-item filtering and live previews of the actual student model. Keyboard/controller navigation and pointer controls are supported. Confirmation shows the exact earned-acorn price and remaining balance. Unlock and Equip are separate actions; the original free outfit can be restored.

Inventory and equipped finish are saved per keeper on this Mac. They are not online purchases or cross-device entitlements; remote players currently see the account's base outfit. Golden memories earn acorns. Existing building and repair costs share the same wallet. Prices: Midnight Scholar 4; Glacier Edition 6; Crimson Legacy 6; Whitehouse Green 8; Twilight Society 8; Golden Hour 10. No payment processor or real-money transaction is involved.

Save format 4 migrates older saves while preserving discoveries, buildings, repairs and courses. Ownership is validated against a fixed six-item catalog. Purchases debit once; failed persistence rolls back ownership and currency, and failed equip saves restore the previous selection. Tests use isolated playtest saves.

## Artwork and design references

The user-provided Elite Dangerous screenshots informed the black/orange panels, cyan store accents and catalog structure. The user-provided Final Fantasy XVI screenshots and https://www.gameuidatabase.com/gameData.php?id=1784&autoload=99394 informed the two-stage opening, serif title, quiet central menu and warm/cool atmosphere. No game screenshot, character or logo from those titles is shipped.

Both original illustration assets were created with Codex's built-in image-generation tool. They are conceptual game artwork, not surveyed Albion architecture or photographs. Store previews are rendered from the existing Unity character, not generated illustrations.

Assets:
- `Unity/Assets/Resources/Presentation/OdysseyKeyArt.png`
- `Unity/Assets/Resources/Presentation/OdysseyTitleArt.png`

### Loading-art prompt

Use case: stylized-concept. Asset type: original widescreen 16:9 cinematic loading key art for the game Albion Odyssey. No text, letters, logos, watermark or interface. Painterly premium adventure-game illustration: a confident young adult Black college explorer wearing a practical deep purple varsity jacket with subtle gold piping and a backpack, waist-up at right third of the composition, holding a small luminous golden acorn. Behind them a beautiful Michigan college campus with red-brick academic buildings and a modest clock tower, autumn oak trees and a wooded river trail fading into mist. Original fictionalized campus atmosphere, not a surveyed architectural reconstruction. Left half is spacious pale silver fog and softly suggested distant architecture with low detail, intentionally clear for large title typography. Dramatic charcoal and silver clouds, restrained amber sparks near the acorn and deep midnight blue shadows. Detailed realistic fabric and face, expressive brushwork, mature cinematic art direction. No armor, swords, fantasy monsters or recognizable existing game characters. Strong wide composition with all important subjects inside central 80 percent for different display crops.

### Title-art prompt

Original widescreen 16:9 title-menu backdrop for Albion Odyssey, a Michigan campus exploration game. No text, letters, typography, logos, buttons or UI. A large exquisite engraved bronze oak-leaf and acorn medallion floats in the upper middle, a natural asymmetrical wreath of oak leaves wrapping a single acorn, hand-etched fine detail, elegant mature premium game artwork. The medallion is subtly lit and low contrast to allow large ivory game title typography to be overlaid over its center. Surrounding scene is mostly deep midnight navy and near black, smoky warm burnt-orange glow from upper left, restrained cool blue glow at right, drifting fine amber embers and tiny blue flecks. Bottom half and especially bottom center is almost empty near-black for a vertical menu. Cinematic, quiet, painterly atmosphere with luminous volumetric mist, a broad gently curving thread of light at the horizon, no architecture, no character, no animals, no existing game imagery. Strong uncluttered negative space, sophisticated restrained illumination, no bright white areas.

## Verification

- `dotnet run --project Tests/UnityRules/Rules.csproj`: migration, wallet conservation, invalid inventory, duplicate purchase rejection, insufficient funds, keeper isolation and 50,000 mixed store/building transactions, plus existing economy/vehicle/school tests.
- Mac build with `Tools/unity_mac.sh build`.
- `-storeSmoke`: isolated save/reload, purchase/equip rollback, live previews, loading/title/store screenshots, pause and title return.
- `-shellSmoke`: existing launch, pause, VR routing, story/video, save recovery and resume flows.
- `-storeSmoke -storeReview`: an isolated interactive review session.
