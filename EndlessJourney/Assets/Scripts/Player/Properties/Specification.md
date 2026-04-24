# Player Properties Specification

## Scope
本文件描述以下两个模块的当前函数职责与流程：
- `PlayerHealth2D`
- `PlayerMana2D`

不包含 UI 展示层（如 `HealthDisplayer` / `ManaDisplay`）。

---

## PlayerHealth2D
文件：`Assets/Scripts/Player/Properties/PlayerHealth2D.cs`

### Public API / Event
| Function / Member | 作用 |
|---|---|
| `CurrentHealth / MaxHealth / IsDead / IsInvincible / HealthNormalized / IsInCombat` | 对外只读状态。 |
| `RegenMultiplier { get; set; }` | 外部可调自然回复倍率（>=0）。 |
| `TakeDamage(float amount)` | 兼容入口，等同 `TakeHarmDamage`。 |
| `TakeHarmDamage(float amount)` | 受伤扣血路径：受无敌限制，可触发入战斗与受击无敌。 |
| `ApplyNonHarmHealthLoss(float amount, bool enterCombat=false)` | 非受伤扣血路径：不受无敌限制，不触发无敌（如 mana out / DoT）。 |
| `ReceiveHarm(float amount, GameObject source)` | `IPlayerHarmful` 接口入口，成功后记录 `LastHarmSource`。 |
| `CanReceiveHarm()` | 返回当前是否可被“受伤类”伤害命中。 |
| `Heal(float amount)` | 治疗，不超过 `maxHealth`。 |
| `SetHealth(float value)` | 直接设血（调试/存档恢复）。 |
| `Revive(bool fullHeal=true)` | 复活并恢复生命。 |
| `SetForcedInCombat(bool inCombat)` | 强制战斗态开关。 |
| `EnterCombat()` | 刷新战斗计时并重置 regen tick。 |
| `OnHealthChanged` | 生命值变化事件（current,max）。 |
| `OnDamaged` | 任意扣血成功时触发（harm 与 non-harm 共用底层）。 |
| `OnNonHarmHealthLost` | 仅 non-harm 扣血成功时触发。 |
| `OnHealed` | 治疗成功时触发。 |
| `OnDied` | 死亡时触发。 |

### Private Function
| Function | 作用 |
|---|---|
| `Awake()` | 初始化闪烁渲染器与初始生命状态。 |
| `Update()` | 驱动无敌计时、战斗计时、自然恢复。 |
| `InitializeHealth()` | 用初始配置设置 `_currentHealth/_isDead` 并发首帧事件。 |
| `StartInvincibility()` | 启动受击无敌与闪烁状态。 |
| `TickInvincibility(float dt)` | 每帧推进无敌/闪烁。 |
| `StopInvincibility(bool restoreVisual)` | 停止无敌并恢复显示。 |
| `CacheFlickerRenderers()` | 缓存受击闪烁使用的 `SpriteRenderer` 与基色。 |
| `ApplyFlickerAlpha(float alphaMultiplier)` | 应用闪烁透明度。 |
| `TickCombatState(float dt)` | 推进离战计时。 |
| `ApplyNaturalRegen(float dt)` | 按 `regenInterval/regenAmount` 做离战恢复。 |
| `Die()` | 处理死亡标记与事件。 |
| `ApplyHealthLossCore(float amount, bool enterCombat, out float appliedDamage)` | 扣血核心（harm / non-harm 共用）。 |
| `OnValidate()` | Inspector 参数约束。 |
| `OnDisable()` | 组件停用时恢复闪烁显示状态。 |

### Health 流程（简版）
1. 外部调用 `TakeHarmDamage` 或 `ApplyNonHarmHealthLoss`。  
2. 两条路径都走 `ApplyHealthLossCore` 扣血。  
3. harm 路径在成功后触发 `StartInvincibility`；non-harm 不触发。  
4. `Update` 中持续处理：无敌倒计时、战斗状态、自然回复。  
5. 若血量降到 0，进入 `Die` 并触发 `OnDied`。

---

## PlayerMana2D
文件：`Assets/Scripts/Player/Properties/PlayerMana2D.cs`

### Public API / Event
| Function / Member | 作用 |
|---|---|
| `CurrentMana / MaxMana / CurrentPotentialMana / MaxPotentialMana / NetMana` | 双槽当前值与上限。 |
| `ManaNormalized / PotentialManaNormalized` | 双槽归一化值。 |
| `ManaExhausting` | `PotentialMana` 未满即视为 exhausting。 |
| `ManaOut` | `PotentialMana <= 0`。 |
| `PotentialManaAllow { get; set; }` | 是否允许进入潜能槽过载消耗。 |
| `ForlornCast { get; set; }` | 过载下是否允许最后一次强制施法（可进入负 mana debt）。 |
| `HasManaDebt` | normal mana 是否为负。 |
| `RegenMultiplier { get; set; }` | 自然恢复倍率。 |
| `HasEnoughMana(float cost)` | 按当前规则判断是否可支付。 |
| `TrySpendMana(float cost)` | 执行消耗：优先 normal，再 potential；forlorn 可进入负债。 |
| `RestoreMana(float amount)` | 回蓝，优先补 `PotentialMana`，再补 normal。 |
| `SetMana(float value)` | 直接设 normal mana。 |
| `SetPotentialMana(float value)` | 直接设 potential mana。 |
| `SetManaState(float manaValue, float potentialManaValue)` | 直接设双槽状态。 |
| `OnManaChanged` | normal 变化事件。 |
| `OnPotentialManaChanged` | potential 变化事件。 |
| `OnManaStateChanged` | 双槽合并变化事件。 |
| `OnManaSpent` | 成功消耗事件。 |
| `OnManaRestored` | 成功恢复事件。 |
| `OnManaOutChanged` | mana out 状态变化事件。 |

### Private Function
| Function | 作用 |
|---|---|
| `Awake()` | 自动尝试绑定 `PlayerHealth2D`，初始化法力。 |
| `Update()` | 每帧执行自然恢复与 mana-out 扣血。 |
| `InitializeMana()` | 应用起始双槽并发初始事件。 |
| `ApplyNaturalRegen(float dt)` | 依据状态计算每秒恢复并分配到双槽。 |
| `ApplyManaOutDamage(float dt)` | `ManaOut` 时对血量施加 non-harm 持续扣血。 |
| `AddRecoveryWithPriority(float amount)` | 恢复核心：先 potential 后 normal。 |
| `RaiseManaEvents()` | 统一发出 mana/potential/state 变化事件。 |
| `NotifyManaOutIfChanged(bool previousState, bool forceNotify=false)` | mana-out 状态变化通知。 |
| `OnValidate()` | Inspector 参数约束。 |

### Mana 流程（简版）
1. 施法前 `HasEnoughMana` 先判断可支付性。  
2. `TrySpendMana` 扣蓝：先 normal，再 potential，必要时（forlorn）进入负 normal。  
3. `Update` 每帧执行 `ApplyNaturalRegen`：  
   - normal 状态：`potential*normalPotentialRate + mana*normalManaRate`  
   - exhausting 状态：`potential*exhaustingRate`  
   - 若 `mana < 0`（debt）则自然回复停用。  
4. `PotentialMana == 0` 时 `ManaOut = true`，`ApplyManaOutDamage` 对 `PlayerHealth2D` 施加 non-harm 伤害。  
5. 所有状态变化通过 `RaiseManaEvents / OnManaOutChanged` 对 UI/系统广播。

---

## 关系图（高层）
1. `SpellCastSystem` 调用 `PlayerMana2D.HasEnoughMana/TrySpendMana`。  
2. `PlayerMana2D` 在 `ManaOut` 时调用 `PlayerHealth2D.ApplyNonHarmHealthLoss`。  
3. `PlayerHealth2D` 统一处理死亡、无敌与自然恢复。  
4. UI 层（`HealthDisplayer/ManaDisplay`）只监听事件，不直接改数值。
