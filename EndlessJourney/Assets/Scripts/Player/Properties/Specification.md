# Player Properties Specification

This document describes the current responsibilities and public flow for:

- `PlayerHealth2D`
- `PlayerMana2D`
- `PlayerArmor2D` at the health integration level
- `ArmorEquipped2D` and `PlayerArmorEquipmentSystem2D` at the equipment backend level
- `PlayerInvincibilityCollision2D` at the damage-invincibility integration level

UI display components such as `HealthDisplayer`, `ManaDisplay`, and `ArmorDisplayer` are not covered here.

## PlayerHealth2D

File: `Assets/Scripts/Player/Properties/PlayerHealth2D.cs`

`PlayerHealth2D` owns player health, harm damage, direct health loss, healing, death, combat-state timing, natural health regeneration, armor resolution, and post-hit invincibility.

### Damage Entry Points

`ReceiveHarm(float amount, GameObject source)`

- Main external entry for enemy/world attacks.
- Respects hit invincibility.
- Applies armor when `applyArmorToHarmDamage` is enabled and `armorSource` is assigned.
- Can enter combat automatically.
- Starts post-hit invincibility when the player survives.
- Records `LastHarmSource`.
- Raises `OnHarmDamaged` and `OnHarmDamageResolved`.

`ReceiveDirectHealthLoss(float amount, bool enterCombat = false)`

- Main external entry for effect/drain damage such as ManaOut, poison, or scripted health loss.
- Ignores hit invincibility.
- Does not apply armor.
- Does not start hit invincibility.
- Can optionally enter combat.
- Raises `OnDirectHealthLost`.

`CanReceiveHarm()`

- Returns false when the player is dead or currently harm-invincible.
- Used by enemy contact/active attack systems before calling `ReceiveHarm`.

### Public State

- `CurrentHealth`
- `MaxHealth`
- `IsDead`
- `IsInvincible`
- `InvincibilityRemaining`
- `HealthNormalized`
- `IsInCombat`
- `RegenMultiplier`
- `LastHarmSource`

### Public Events

- `OnHealthChanged(float current, float max)`
- `OnDamaged(float appliedDamage)`
- `OnHarmDamaged(float incomingDamage, GameObject source)`
- `OnHarmDamageResolved(float incomingDamage, float finalDamage, float armorAbsorbed, GameObject source)`
- `OnInvincibilityChanged(bool isInvincible)`
- `OnDirectHealthLost(float appliedDamage)`
- `OnHealed(float appliedHeal)`
- `OnDied()`

### Other Public Operations

- `Heal(float amount)`
- `SetHealth(float value)`
- `Revive(bool fullHeal = true)`
- `SetForcedInCombat(bool inCombat)`
- `EnterCombat()`

### Health Flow

```text
Enemy/world attack
-> ReceiveHarm(amount, source)
-> CanReceiveHarm gate
-> ApplyHarmDamage
-> optional armor reduction
-> ApplyHealthLossCore
-> StartInvincibility if alive
-> broadcast harm/resolved events
```

```text
ManaOut / poison / scripted drain
-> ReceiveDirectHealthLoss(amount, enterCombat)
-> ApplyHealthLossCore
-> broadcast direct health loss event
```

### Armor Integration

`PlayerHealth2D` does not own armor values. It only asks `PlayerArmor2D` to resolve incoming harm damage.

Current armor rule:

- Armor has durability and reduction efficiency.
- Harm damage is reduced by armor while armor is not broken.
- The final hit still receives full configured reduction even if remaining durability is lower than the absorbed amount.
- After that hit, armor durability reaches zero and the armor becomes broken.
- Broken armor no longer reduces harm damage.

### Armor Equipment Backend

`ArmorData` defines a small set of armor variants. Current armor data is intentionally limited to:

- stable armor id
- display data
- max durability
- damage reduction efficiency

`ArmorLibrary2D` owns armor asset lookup and unlock state.

`ArmorEquipped2D` owns the currently equipped armor id and saves it to `record.json`.

`PlayerArmorEquipmentSystem2D` bridges equipment data into `PlayerArmor2D`:

```text
ArmorEquipped2D equippedArmorId
-> ArmorLibrary2D resolves ArmorData
-> PlayerArmorEquipmentSystem2D.ApplyEquippedArmorToRuntime
-> PlayerArmor2D.ApplyArmorStats(maxDurability, reduction, restoreFull)
```

Inspector requirements:

- Assign `ArmorLibrary2D` on `ArmorEquipped2D`.
- Assign `ArmorEquipped2D` and `PlayerArmor2D` on `PlayerArmorEquipmentSystem2D`.
- Assign `PlayerArmorEquipmentSystem2D` on `OpenForge2D` only when Forge should repair armor on open.

Current limitation:

- Armor backend exists, but Armor Forge UI is not implemented yet.
- ArmorData assets and scene/prefab bindings still require Unity verification.

## PlayerMana2D

File: `Assets/Scripts/Player/Properties/PlayerMana2D.cs`

`PlayerMana2D` owns the dual-pool mana model:

- `Mana`: normal cast resource.
- `PotentialMana`: overload reserve and ManaOut gate.

It handles spending, restoring, natural regeneration, external natural-regen blocking, and ManaOut health drain.

### Public State

- `CurrentMana`
- `MaxMana`
- `CurrentPotentialMana`
- `MaxPotentialMana`
- `NetMana`
- `ManaNormalized`
- `PotentialManaNormalized`
- `ManaExhausting`
- `ManaOut`
- `PotentialManaAllow`
- `ForlornCast`
- `HasManaDebt`
- `AllowNaturalRegen`
- `RegenMultiplier`
- `IsNaturalRegenBlockedExternally`

### Public Events

- `OnManaChanged(float current, float max)`
- `OnPotentialManaChanged(float current, float max)`
- `OnManaStateChanged(float manaCurrent, float manaMax, float potentialCurrent, float potentialMax)`
- `OnManaSpent(float spentAmount)`
- `OnManaRestored(float restoredAmount)`
- `OnManaOutChanged(bool isManaOut)`

### Public Operations

`HasEnoughMana(float cost)`

- Checks whether current rules allow the cost to be paid.
- Without overload, only normal mana is usable.
- With overload, `Mana + PotentialMana` is usable.
- With `ForlornCast`, one forced cast can create normal mana debt before debt already exists.

`TrySpendMana(float cost)`

- Spends normal mana first.
- Then spends potential mana if overload is allowed.
- In forlorn mode, unresolved cost can become negative normal mana debt.

`RestoreMana(float amount)`

- Restores `PotentialMana` first.
- Then restores normal `Mana`.
- Works even when `AllowNaturalRegen` is false.

`SetMana(float value)`

- Directly sets normal mana.

`SetPotentialMana(float value)`

- Directly sets potential mana.

`SetManaState(float manaValue, float potentialManaValue)`

- Directly sets both pools.

`SetNaturalRegenBlocked(bool blocked)`

- Temporary external block for natural mana regeneration.
- Used by spell singing so active casting can stop passive recovery.

### Natural Regeneration

Natural mana regeneration is skipped when:

- `AllowNaturalRegen` is false.
- `SetNaturalRegenBlocked(true)` is active.
- normal mana is below zero.
- calculated regeneration is zero.

Important distinction:

```text
AllowNaturalRegen / SetNaturalRegenBlocked
-> only affect passive regen
-> do not block RestoreMana()
-> do not block ManaOnHit inscription restore
```

### ManaOut Flow

```text
PotentialMana <= 0
-> ManaOut = true
-> ApplyManaOutDamage
-> PlayerHealth2D.ReceiveDirectHealthLoss
```

ManaOut damage is direct health loss, so it does not trigger hit invincibility and does not apply armor.

## Integration Notes

### Enemy Damage

Enemy contact and active melee attacks should use:

```csharp
playerHealth.ReceiveHarm(damage, enemyGameObject);
```

or the `IPlayerHarmful` interface.

### Effect Damage

Effect/drain damage should use:

```csharp
playerHealth.ReceiveDirectHealthLoss(amount, enterCombat);
```

### Invincibility Collision

`PlayerInvincibilityCollision2D` listens to `PlayerHealth2D.OnInvincibilityChanged`.

When enabled and configured, it can temporarily ignore physics collision between player layers and enemy layers during hit invincibility. This prevents the player from being trapped in enemy bodies while invincible.

Inspector requirements:

- Assign `PlayerHealth2D`.
- Set `playerCollisionLayers`, usually `PlayerSide`.
- Set `enemyCollisionLayers`, usually `Enemy`.
