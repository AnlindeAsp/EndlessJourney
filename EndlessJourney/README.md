# EndlessJourney (Unity 2D Prototype)

## 项目简介
`EndlessJourney` 是一个以 **2D platformer + action combat** 为核心的 Unity 原型项目。  
当前阶段聚焦于：
- 手感优先的角色移动（game feel first）
- 可扩展的资源系统（Health / Mana）
- 可扩展的战斗骨架（WeaponData / CombatCore / Melee / Spell）

> 目前仍是原型阶段，暂不关注剧情、UI 完整包装、敌人行为树、关卡推进系统。

## 技术栈
- Unity 2D
- C# (Inspector-driven 参数配置)
- Rigidbody2D 物理驱动

## 目录概览（Scripts）
- `Assets/Scripts/Interfaces`
  - `IPlayerHarmful.cs`：玩家受伤统一接口（ReceiveHarm/CanReceiveHarm）
  - `IDamageable2D.cs`：兼容旧通道的受击接口
  - `IHittable.cs`：可受击目标统一接口（新通道）
- `Assets/Scripts/Player`
  - `Movement/`
    - `PlayerCore2D.cs`：玩家共享上下文（Rigidbody2D / Ground / Facing / movement lock）
    - `PlayerInput2D.cs`：统一输入读取（New Input System + Legacy fallback）
    - `GroundCheck2D.cs`：地面检测
    - `PlayerMovement2D.cs`：基础移动、jump、coyote、jump buffer、apex/fall/low-jump
    - `PlayerDash2D.cs`：冲刺模块
    - `PlayerDoubleJump2D.cs`：二段跳模块
  - `Combat/`
    - `SpellCastSystem.cs`：施法（learned / cast time / cooldown / mana cost）
    - `PlayerCombatCore.cs`：战斗快照数据（攻击距离、伤害、攻速等）
    - `PlayerWeaponSystem.cs`：装备武器并把公式结果写入 CombatCore
    - `PlayerMeleeAttack2D.cs`：近战（前向三角判定、持续窗口、调试显示）
  - `Properties/`
    - `PlayerHealth2D.cs`：生命值、受伤/非受伤扣血、回复、死亡、自然回复
    - `PlayerMana2D.cs`：双槽法力（Mana / PotentialMana）、过载与恢复逻辑
- `Assets/Scripts/Combat`
  - `HitContext.cs`：命中上下文数据（来源、伤害、方向、点位、类型）
  - `HitResult.cs`：命中处理结果数据
  - `HitType.cs`：命中类型枚举（Melee/Spell/Projectile/Environment）
  - `HittableBase.cs`：可选受击基类（无敌、命中冷却、自身命中屏蔽）
- `Assets/Scripts/Enemy`
  - `AI/`
    - `EnemyCore2D.cs`：敌人核心上下文（Rigidbody2D / Hittable / Facing / SpawnPosition）
    - `EnemyBase2D.cs`：敌人行为基类（通过 Core 访问共享状态与动作）
    - `EnemyPatrolWalker2D.cs`：简单巡逻（遇墙反向、持续走动）
    - `EnemyBlackboard2D.cs`：敌人共享记忆（目标、可见性、状态）
    - `EnemyPerception2D.cs`：索敌传感器（半径/FOV/视线）
    - `EnemyBrainFSM2D.cs`：状态机决策（Patrol/Chase/Attack/Return）
  - `Combat/`
    - `EnemyContactAttack2D.cs`：接触伤害核心（冷却、层过滤、目标扣血）
    - `EnemyContactDamageZone2D.cs`：子物体触发区转发脚本（推荐用于放大触碰伤害区域）
  - `Properties/`
    - `EnemyHittable.cs`：最小敌人受击实现（血量、受伤、死亡）
- `Assets/Scripts/Weapon`
  - `WeaponData.cs`：武器 ScriptableObject（类型/长度/锋利/重量/状态）
- `Assets/Scripts/UI`
  - `ManaDisplay.cs`：法力条显示（TMP 文本 + 颜色状态）
  - `HealthDisplayer.cs`：生命条显示
- `Assets/Scripts/Camera`
  - `SimpleCameraFollow2D.cs`：简易相机跟随

## 默认输入（当前）
- 移动：`A / D`（或方向键左右）
- 跳跃：`Space / W / Up`
- 冲刺：`LeftShift / RightShift`
- 近战：`F`
- 施法：`C`

## 快速开始（最小场景）
1. 创建 `Player` 物体并挂：
   - `Rigidbody2D`
   - `Collider2D`（Box 或 Capsule）
   - `PlayerInput2D`
   - `PlayerCore2D`
   - `PlayerMovement2D`
   - `PlayerDash2D`
   - `PlayerDoubleJump2D`
   - `PlayerHealth2D`
   - `PlayerMana2D`
   - `SpellCastSystem`
   - `PlayerCombatCore`
   - `PlayerWeaponSystem`
   - `PlayerMeleeAttack2D`
2. 在 Player 子物体创建 `GroundCheck`（空物体）并挂 `GroundCheck2D`。
3. 配置地面 Layer，并在 `GroundCheck2D` / 相关脚本中设置检测层。
4. 创建至少一个 `WeaponData` 资源并装备到 `PlayerWeaponSystem`。
5. 在 `PlayerCombatCore` 确认攻击参数非 0（或通过武器自动计算得到）。
6. 进入 Play，按 `F` 测近战、按 `C` 测施法。

## Log

### 2026-04-23
今日主要成果：
- 完成并稳定了玩家基础动作链：移动、跳跃手感优化、dash、double jump。
- 完成 `PlayerCore` 化整合思路：共享引用与状态由 Core 提供，能力脚本模块化。
- 完成生命/法力系统草稿并扩展：
  - `Health`：伤害、治疗、死亡、脱战自然回复（可调 multiplier）
  - `Mana`：双槽（`Mana + PotentialMana`）、过载、枯竭、副作用与恢复优先级
  - 支持负法力可显示与法力相关 UI 联动
- 完成 `SpellCastSystem`：支持 learned gate、cast time、cooldown、法力消耗。
- 完成武器与战斗快照基础：
  - `WeaponData`（ScriptableObject）
  - `PlayerWeaponSystem` 读取武器公式并写入 `PlayerCombatCore`
- 完成 `MeleeAttack` 原型：
  - 前向等腰三角命中感（左右攻击）
  - 近战判定窗口持续时间（当前默认 1 秒）
  - 命中日志（`hi target`）
  - 攻击成功日志（便于排障）
  - 运行时调试可视化（Debug 线框 + Game View LineRenderer）
- 完成 `Hittable` 架构落地：
  - 新增 `IHittable + HitContext + HitResult + HitType`
  - 新增 `HittableBase` 作为通用受击门控层
  - 新增 `EnemyHittable` 作为敌人最小可用受击模块
  - `PlayerMeleeAttack2D` 优先走 `IHittable`，并保留旧接口兼容
  - 修正 Gizmo 原点与真实命中原点一致（offset 观察更直观）
- 完成敌人 AI 原型骨架：
  - 新增 `EnemyPerception2D`（索敌）
  - 新增 `EnemyBlackboard2D`（记忆与共享状态）
  - 新增 `EnemyBrainFSM2D`（FSM 决策）
  - 保持与现有 `EnemyPatrolWalker2D / EnemyContactAttack2D` 可组合

主要记忆（给后续开发）:
- 输入统一从 `PlayerInput2D` 读取，功能脚本不要各自直接读键盘。
- 战斗数值以 `PlayerCombatCore` 为单一读入口，避免多处重复公式。
- 近战已支持“无可用武器但 CombatCore 有有效伤害值时也可测试”，便于原型调参。
- 敌人侧建议以 `EnemyCore2D` 为模块汇聚点：感知/FSM/接触攻击/巡逻都只通过 Core 读写共享状态。
- 外部 `dotnet build` 结果可能受 Unity 生成的 `csproj` 刷新状态影响；以 Unity Editor 内编译状态为准。
- Player 扣血已拆分两条通道：`Harm`（触发无敌）与 `NonHarm`（不触发无敌，如 ManaOut/DoT）。

### 2026-04-25
根据 git 提交还原：
- 开始完善近战后坐力与攻击方向系统：
  - 新增/调整 `PlayerAttackRecoil2D`
  - 新增 `AttackDirection2D` 与 `PlayerAttackDirectionResolver2D`
  - 横向攻击 recoil 进入较稳定状态
- 修复向上攻击引入时的 recoil 与 offset 问题。
- `PlayerMeleeAttack2D` 开始从单纯横向攻击扩展到更明确的方向攻击结构。
- `SimpleCameraFollow2D` 与 TestingGround 场景配置进入基础可用状态。

### 2026-04-26
根据 git 提交还原：
- 完成并测试向下攻击逻辑：
  - 下劈命中后可恢复 dash / extra jump
  - 下劈 recoil 与跳跃/重力手感做过平衡
- 修正向上攻击 offset 的问题。
- `PlayerDash2D / PlayerDoubleJump2D / PlayerMovement2D` 与近战命中奖励开始联动。

### 2026-04-27
根据 git 提交还原：
- 敌人受击反馈进入第一版：
  - 新增/整理 `EnemyHitReaction2D`
  - 受击后可产生 stun 与 knockback
  - 敌人 AI / Blackboard / FSM / Patrol 与受击反馈开始组合
- 玩家攻击 recoil 与敌人 hit reaction 做过一次整体平衡。

### 2026-04-29
根据 git 提交还原：
- 基础 gameplay 场景与 prefab 进入可玩状态：
  - 新增 player / enemy / map / camera / canvas 等 prefab
  - TestingGround 与 trial 场景有基础布置
- 新增或整理敌人基础模块：
  - `EnemyCore2D`
  - `EnemyDeathBehaviour2D`
  - `EnemyStabler2D`
  - floating enemy 原型
- 完成暂停菜单与按键设置初版：
  - `PauseMenuController2D`
  - `KeybindSettingsController2D`
  - `PlayerInput2D` 与 keybind 逻辑联动

### 2026-04-30
根据 git 提交还原：
- 新增交互与拾取体系基础：
  - `IInteractable`
  - `IPickable`
  - `TriggerInteractable2D`
  - `ReadInteractable2D`
  - `PlayerInteractor2D`
- 新增能力拾取物：
  - `AllowDashPickup2D`
  - `AllowDoubleJumpPickup2D`
  - `AllowSpellCastPickup2D`
  - `PlayerAbilityCore2D`
- 这一天奠定了后续“统一 R 交互”和能力解锁的基础。

### 2026-05-02
根据 git 提交还原：
- 法术系统进入基础版本：
  - `SpellData2D`
  - `SpellEffectData2D`
  - `DamageSpellEffectData2D`
  - `HealSpellEffectData2D`
  - `BuffSpellEffectData2D`
  - `SpellCastSystem`
  - `SpellBook2D`
  - `SpellSlotCaster2D`
  - `SpellLibrary2D`
  - `SpellRecordStore2D`
- Projectile 系统进入基础版本：
  - `PlayerProjectile2D`
  - `PlayerProjectileLauncher2D`
  - `ProjectileMovement2D`
- 新增 `record.json` 与初始 spell / weapon asset。
- 同时开始把 melee、projectile、spell 目录拆开，战斗代码结构进一步清晰。

### 2026-05-03
根据 git 提交还原：
- 完成存档点逻辑并测试：
  - `OpenSavingLibrary2D`
  - interactable 区域与打开 storage UI 的链路建立
- Player 战斗代码目录整理：
  - `Combat-Core`
  - `Combat-Melee`
  - `Combat-Spell`
  - `Libraries`
- 新增 `WeaponEquipped2D` 与 `WeaponLibrary2D`，武器装备状态进入 record 流程。
- 新增 `PlayerRecordStore2D`，spell / weapon 的 record 读写开始统一。
- 新增 `GameCanvasManager2D` 与 `StorageCanvasController2D`，UI 状态管理开始集中。
- Storage UI 初步支持 Weapon / Spell 页面。

### 2026-05-04
根据 git 提交还原：
- Weapon Page UI 进入可用版本：
  - `WeaponPageController2D`
  - `WeaponPageDisplayer2D`
  - `WeaponPageRow2D`
  - `WeaponListPrefab`
- 武器列表、选中态、装备态、锁定态与 weapon detail 显示开始成型。
- `WeaponData` 与 weapon asset 补充 UI 展示需要的数据。

### 2026-05-05
根据 git 提交还原：
- Spell UI 完成基础版本并多次迭代：
  - `SpellPageController2D`
  - `SpellPageDisplayer2D`
  - `SpellPageSlotButton2D`
  - `SpellPageSpellNameRow2D`
- Spell 页面逐步形成“法术书页 + 法术名称列表 + 详情预览 + 写入/擦除”的结构。
- 新增更多法术与 projectile：
  - geowave
  - pyroblast
  - shadow lance
  - recovery / heal 类 spell
- 新增 `PlayerDataStore2D` 与 `PlayerData.json`，玩家能力进度开始从 record 外拆到 player data。
- Armor 进入第一版：
  - `PlayerArmor2D`
  - `ArmorDisplayer`
  - `PlayerHealth2D` 与 armor 结算开始联动
- UI 目录进一步整理为 `Storage/Spell` 与 `Storage/Weapon`。

### 2026-05-07
根据 git 提交还原：
- 玩家攻击动画进入第一版：
  - 新增 `PlayerMeleeAttackAnimator2D`
  - `PlayerMeleeAttack2D` 发出攻击方向事件供动画播放
- 修复攻击动画导致的攻击碰撞判定异常。
- 新增多把武器 asset：
  - Elder Wood
  - Lyka'sWill
  - Only
  - RoyalMark

### 2026-05-10
根据 git 提交还原：
- 修复 player attack hitbox 过度影响其它触发区域的问题：
  - `TriggerInteractable2D`
  - `AbilityPickupItem2D`
  - `EnemyContactAttack2D`
  - `PlayerMeleeAttackAnimator2D`
- 新增向上攻击动画素材与 `upSlash.anim`。
- `PlayerMeleeAttackAnimator2D` 扩展 Up / Down 动画播放和方向处理。

### 2026-05-11
根据 git 提交还原：
- 完成 dual wielding 能力与装备逻辑：
  - `PlayerAbilityCore2D` 增加 dual wielding
  - `WeaponData` 增加 dual wieldable 相关数据
  - `WeaponEquipped2D` 保存 dual wielding mode
  - `PlayerWeaponSystem` 引入 effective weapon type
  - weapon UI 增加 dual wielding 操作
- 新增 dual attack animation 控制：
  - `PlayerMeleeAttackAnimationController2D`
  - 支持双持时主/副攻击动画播放
- 新增 `EnemySpawner2D` 与 boss / enemy 生成测试相关内容。
- TestingGround 场景有一次较大的整合更新。

### 2026-05-13
阶段性开发整理：
- 整理 Player 战斗相关目录：
  - 核心战斗数据迁移到 `Player/Combat-Core`
  - `WeaponLibrary2D / SpellLibrary2D / PlayerRecordStore2D` 统一放入 `Player/Libraries`
  - 武器、法术、战斗核心职责进一步拆开，避免全部堆在 melee 逻辑里
- 完成存档点与 UI 状态管理基础：
  - `TriggerInteractable2D` 增加统一世界提示文字
  - `OpenSavingLibrary2D` 通过 `GameCanvasManager2D` 打开仓库/存档 UI
  - `ESC / R` 关闭当前 gameplay canvas，避免 ESC 在任意界面都直接打开 pause menu
- 完成 Storage UI 初版：
  - `StorageCanvasController2D` 管理 Weapon / Spell 页面切换
  - `WeaponPageController2D + WeaponPageDisplayer2D` 支持武器列表、选中显示、装备、双持切换
  - `SpellPageController2D + SpellPageDisplayer2D` 改为法术书页面逻辑，支持 1-5 页、预览、写入、擦除
  - 新增 `Assets/Scripts/UI/UI_controller.md` 记录 controller/displayer 职责
- 完成玩家进度数据基础：
  - 新增 `PlayerData.json` 记录能力解锁状态
  - `PlayerAbilityCore2D` 从 PlayerData 读取 dash / double jump / spell cast / dual wielding 等能力
- 完成盔甲系统：
  - 盔甲有 durability 与 damage reduction
  - 最后一击即使耐久不足也完整减伤，之后进入 broken 状态
  - 新增 `ArmorDisplayer`
- 扩展武器系统：
  - `WeaponData` 增加 detail image、description、dual wieldable 等字段
  - `WeaponEquipped2D` 支持 dual wielding mode 并保存到 record
  - `PlayerWeaponSystem` 引入 effective weapon type，单手剑可在双持能力解锁后临时视为 DualBlades
- 攻击动画进入第一版：
  - `PlayerMeleeAttackAnimator2D` 播放 Forward / Up / Down 攻击动画
  - 支持左右镜像、上下攻击动画、范围缩放与双持时双动画播放控制
  - `PlayerMeleeAttackAnimationController2D` 统一调度主/副攻击动画
- 扩展战斗命中上下文：
  - 新增 `PlayerCombatRuntime2D`
  - `HitContext` 增加 `DamageType / WeaponType / WeaponWeight / HitIndex / HitCount`
  - `PlayerMeleeAttack2D` 从 runtime 获取最终近战快照并传给 enemy
  - `EnemyHitReaction2D` 击退改为主要受 weapon weight / weapon type 影响
- 新增 `EnemySpawner2D` 原型：
  - 支持 interval、on enable、on hit 触发生成
  - 支持最大存活数量与多个 spawn point

### 2026-05-21
今日主要成果：
- 完成武器铭文系统第一版：
  - 新增 `WeaponInscriptionData`
  - 新增 `WeaponInscriptionLibrary2D`
  - `WeightMultiplier / SharpnessMultiplier` 已实际参与武器 weight / sharpness 计算
  - `ComboDamageRamp / MissingHealthDamageBonus / ManaOnHit` 先作为数据占位，后续接动态战斗效果
- 铭文存档从“全局一个铭文”改为“每把武器独立铭文”：
  - record 新增 `weaponInscriptionIds`
  - 结构为 `weaponId -> inscriptionId`
  - 保留旧字段 `equippedWeaponInscriptionId` 作为迁移兼容
- 完成铁匠铺铭文 UI 第一版：
  - 新增 `GameCanvasState2D.Forge`
  - 新增 `OpenForge2D`
  - 新增 `WeaponInscriptionPageController2D`
  - 新增 `WeaponInscriptionPageDisplayer2D`
  - 新增 weapon row / inscription row prefab 脚本
  - Forge 页面支持选武器、选铭文、`Space` 篆刻、`Backspace/Delete` 擦除、`R/ESC` 关闭
- 修正铭文即时生效链路：
  - `PlayerWeaponSystem` 监听 `OnWeaponInscriptionChanged`
  - 如果变更的是当前装备武器，立即 `RecalculateCombatSnapshot`
  - 确认 Inspector 需要手动绑定 `PlayerWeaponSystem -> Inscription Equipped`
- 修正 `WeaponInscriptionLibrary2D` 初始化顺序风险：
  - `HasInscription / GetInscriptionData` 调用前会确保索引已构建
  - 避免 Library `Awake` 顺序导致已存在铭文被误判为 unknown
- `EnemyHittable` 增加可开关受击伤害 log：
  - 显示实际 applied damage、剩余 HP、damage type、weapon type、hit index
- 完成剩余动态铭文效果：
  - `ComboDamageRamp`：连续有效近战命中叠加伤害，超时或切换武器/铭文时重置
  - `MissingHealthDamageBonus`：按玩家已损失生命百分比动态提升近战伤害
  - `ManaOnHit`：有效近战命中后通过 `PlayerMana2D.RestoreMana` 恢复法力
  - `PlayerCombatRuntime2D` 负责运行时最终伤害计算与命中后铭文回调
- `PlayerMana2D` 增加 `Allow Natural Regen`：
  - 可单独关闭自然回蓝
  - 不影响主动恢复、铭文回蓝、消耗与 ManaOut 掉血
- 敌人死亡关停逻辑增强：
  - `EnemyDeathBehaviour2D` 新增 `behavioursToDisable`
  - 死亡时可手动关闭 AI、接触攻击、感知、spawner、Boss 行为等生前逻辑
  - 用于修复 Boss 死亡后仍继续生成敌人的问题
- 玩家受击无敌逻辑增强：
  - `PlayerHealth2D` 新增 `OnInvincibilityChanged` 与 `InvincibilityRemaining`
  - 新增 `PlayerInvincibilityCollision2D`
  - 受伤无敌期间可临时忽略 Player / Enemy layer 物理碰撞，避免玩家被敌人夹住
  - 碰撞恢复同时依赖事件与每帧状态校验，避免 layer collision 永久残留
- 今日验证：
  - `dotnet build .\Assembly-CSharp.csproj` 通过
  - 仅剩 Unity Inspector 序列化字段相关 warning

## 下一步建议（可选）
- 将近战从“调试线框”升级为正式攻击表现（动画事件 + 命中特效 + hit stop）。
- 增加敌人受击反馈（硬直、击退、受击无敌帧）。
- 将输入切换到可重绑定的 `Input Actions` 资产。
- 为核心模块补最小 PlayMode 测试（移动、资源、施法、近战）。
