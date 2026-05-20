# UI Controller Overview

This document summarizes the current UI controller flow and responsibilities.

## High-Level Flow

World interaction and UI state are separated.

```text
Player presses R in an interactable zone
-> PlayerInteractor2D selects the best IInteractable
-> OpenSavingLibrary2D requests GameCanvasManager2D
-> GameCanvasManager2D opens SavingLibrary canvas
-> StorageCanvasController2D opens the default storage page
-> PageController handles player operations
-> PageDisplayer renders visible UI
```

`ESC` is routed by `GameCanvasManager2D`.

```text
Gameplay + ESC -> open PauseMenu
SavingLibrary + ESC -> close SavingLibrary
SavingLibrary + R -> close SavingLibrary
Forge + ESC -> close Forge
Forge + R -> close Forge
PauseMenu + ESC -> close PauseMenu
```

## GameCanvasManager2D

Location: `Assets/Scripts/System/GameCanvasManager2D.cs`

Owns the current high-level UI state:

- `Gameplay`
- `PauseMenu`
- `SavingLibrary`
- `Forge`
- `Inventory`
- `Map`
- `Settings`

Responsibilities:

- Route global canvas input such as `ESC`.
- Open and close high-level canvases.
- Prevent pause menu from opening while another gameplay canvas is active.
- Lock/unlock player movement or actions while gameplay canvases are open.
- Show/restore cursor for gameplay canvases.

It should not handle weapon, spell, inventory, or map page details.

## PauseMenuController2D

Location: `Assets/Scripts/System/PauseMenuController2D.cs`

Responsibilities:

- Show/hide pause menu root.
- Pause/resume time.
- Show pause main panel and settings panel.
- Lock player movement while paused.

`GameCanvasManager2D` should disable its direct `ESC` toggle by calling:

```csharp
pauseMenuController.SetEscapeToggleEnabled(false);
```

This keeps `ESC` routing centralized.

## OpenSavingLibrary2D

Location: `Assets/Scripts/Interaction/OpenSavingLibrary2D.cs`

Responsibilities:

- Inherit trigger/prompt behavior from `TriggerInteractable2D`.
- Respond to player world interaction.
- Request `GameCanvasManager2D` to open the saving library.
- Hide or refresh the world prompt when UI state changes.

It should not directly open canvas roots, lock player state, or handle cursor state.

## OpenForge2D

Location: `Assets/Scripts/Interaction/OpenForge2D.cs`

Responsibilities:

- Inherit trigger/prompt behavior from `TriggerInteractable2D`.
- Respond to player world interaction at a forge / blacksmith point.
- Request `GameCanvasManager2D` to open the forge canvas.
- Hide or refresh the world prompt when UI state changes.

It should not directly edit weapon inscription data.

## WeaponInscriptionPageController2D

Location: `Assets/Scripts/UI/Forge/Inscription/WeaponInscriptionPageController2D.cs`

Handles player operations on the forge inscription page.

Responsibilities:

- Read available weapons from `WeaponLibrary2D`.
- Read available inscriptions from `WeaponInscriptionLibrary2D`.
- Read and write per-weapon inscription mapping through `WeaponInscriptionEquipped2D`.
- Track selected weapon id and selected inscription id.
- Let `A/D` switch focus between weapon list and inscription list.
- Let `W/S` move within the focused list.
- Let `Space` engrave the selected inscription onto the selected weapon.
- Let `Backspace/Delete` erase the selected weapon's inscription.
- Ask `WeaponInscriptionPageDisplayer2D` to render updated UI.

It should not directly create UI rows or write display text.

## WeaponInscriptionPageDisplayer2D

Location: `Assets/Scripts/UI/Forge/Inscription/WeaponInscriptionPageDisplayer2D.cs`

Renders the forge inscription page.

Responsibilities:

- Spawn weapon rows and inscription rows.
- Render selected weapon details and current inscription.
- Render selected inscription details.
- Bind Engrave and Erase buttons to controller-provided callbacks.
- Show which list currently has keyboard focus.

It should not directly save records or change weapon inscription state.

## WeaponInscriptionWeaponRow2D

Location: `Assets/Scripts/UI/Forge/Inscription/WeaponInscriptionWeaponRow2D.cs`

Represents one selectable weapon row in the forge inscription page.

Responsibilities:

- Display weapon name, icon, and current inscription name.
- Show selected, equipped, engraved, and locked indicators.
- Invoke the provided weapon-selection callback when clicked.

It should not know about `WeaponLibrary2D` or write record data.

## WeaponInscriptionChoiceRow2D

Location: `Assets/Scripts/UI/Forge/Inscription/WeaponInscriptionChoiceRow2D.cs`

Represents one selectable inscription row in the forge inscription page.

Responsibilities:

- Display inscription name and effect type.
- Show selected and engraved-on-current-weapon indicators.
- Invoke the provided inscription-selection callback when clicked.

It should not know about `WeaponInscriptionEquipped2D`.

## StorageCanvasController2D

Location: `Assets/Scripts/UI/Storage/StorageCanvasController2D.cs`

Controls pages inside the saving library canvas.

Responsibilities:

- Choose the default storage page.
- Show one storage page root at a time.
- Call the active page controller to refresh.
- Let `A/D` or left/right arrows switch storage pages.

Current pages:

- `Weapon`
- `Spell`

It should not directly edit weapon or spell data.

## WeaponPageController2D

Location: `Assets/Scripts/UI/Storage/Weapon/WeaponPageController2D.cs`

Handles player operations on the weapon page.

Responsibilities:

- Read weapon list/unlock state from `WeaponLibrary2D`.
- Read equipped state from `WeaponEquipped2D`.
- Track selected weapon id.
- Equip selected weapon.
- Ask `WeaponPageDisplayer2D` to render updated UI.

It should not directly create UI rows or write visual text.

## WeaponPageDisplayer2D

Location: `Assets/Scripts/UI/Storage/Weapon/WeaponPageDisplayer2D.cs`

Renders the weapon page.

Responsibilities:

- Spawn weapon row prefabs.
- Render selected weapon details.
- Update weapon icon, stats, locked/unlocked/equipped state.
- Bind the equip button to a controller-provided callback.

It should not directly save records or change equipped weapon state.

## WeaponPageRow2D

Location: `Assets/Scripts/UI/Storage/Weapon/WeaponPageRow2D.cs`

Represents one weapon row in the weapon list.

Responsibilities:

- Display weapon name and state.
- Show selected/equipped/locked indicators.
- Invoke the provided selection callback when clicked.

It should not know about `WeaponLibrary2D` or `WeaponEquipped2D`.

## SpellPageController2D

Location: `Assets/Scripts/UI/Storage/Spell/SpellPageController2D.cs`

Handles player operations on the spell book page.

Responsibilities:

- Read available spell definitions and unlock state from `SpellLibrary2D`.
- Read written page state from `SpellBook2D`.
- Treat spell slots as spell book pages.
- Let number keys `1-5` turn to available pages.
- Let mouse wheel or `W/S` move through the spell name list.
- Write the selected preview spell to the current page.
- Erase the current page, allowing spell pages to stay blank.
- Ask `SpellPageDisplayer2D` to render updated UI.

It should not directly create UI rows or edit display text.

## SpellPageDisplayer2D

Location: `Assets/Scripts/UI/Storage/Spell/SpellPageDisplayer2D.cs`

Renders the spell book page.

Responsibilities:

- Spawn page button prefabs.
- Spawn spell name row prefabs.
- Render current page number and written spell state.
- Render left-side spell data and right-side spell description.
- Slowly blink the description area when showing an unwritten preview spell.
- Bind Write and Erase buttons to controller-provided callbacks.

It should not directly save records or change `SpellBook2D`.

## SpellPageSpellNameRow2D

Location: `Assets/Scripts/UI/Storage/Spell/SpellPageSpellNameRow2D.cs`

Represents one selectable spell name in the spell list.

Responsibilities:

- Display only the spell name.
- Show selected, written-on-current-page, written-elsewhere, and locked indicators.
- Invoke the provided selection callback when clicked.

It should not know about `SpellLibrary2D` or `SpellBook2D`.

## SpellPageSlotButton2D

Location: `Assets/Scripts/UI/Storage/Spell/SpellPageSlotButton2D.cs`

Represents one spell book page button. Each page maps to one `SpellBook2D` slot.

Responsibilities:

- Display page number.
- Display the spell currently written on that page, or `Blank`.
- Show selected and unavailable states.
- Invoke the provided page-selection callback when clicked.

It should not write or erase spells by itself.

## Data Responsibility

Weapon-related state is split like this:

```text
WeaponData
-> static weapon configuration

WeaponLibrary2D
-> all weapon assets
-> unlock state

WeaponEquipped2D
-> currently equipped weapon id
-> save equipped weapon id to record
-> does not clear equipment; the player should always keep one weapon equipped

PlayerWeaponSystem
-> convert equipped WeaponData into PlayerCombatCore stats

WeaponPageController2D
-> UI operations

WeaponPageDisplayer2D
-> UI rendering
```

Library classes should not know about UI pages. Display classes should not write gameplay state.

Spell-related state is split like this:

```text
SpellData2D
-> static spell configuration
-> book description and supplementary note

SpellLibrary2D
-> all spell assets
-> unlock state

SpellBook2D
-> spell book pages / spell slots
-> written spell ids
-> save written spell ids to record
-> pages may be blank

SpellCastSystem / SpellSlotCaster2D
-> cast spells from written spell slots

SpellPageController2D
-> UI operations

SpellPageDisplayer2D
-> UI rendering
```

Weapon inscription state is split like this:

```text
WeaponInscriptionData
-> static inscription configuration

WeaponInscriptionLibrary2D
-> all inscription assets
-> resolves inscription id to asset

WeaponInscriptionEquipped2D
-> per-weapon inscription mapping
-> saves weaponId -> inscriptionId to record

PlayerWeaponSystem
-> reads the currently equipped weapon's inscription
-> converts static inscription effects into combat snapshot values

WeaponInscriptionPageController2D
-> forge UI operations

WeaponInscriptionPageDisplayer2D
-> forge UI rendering
```
