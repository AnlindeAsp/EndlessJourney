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

## 下一步建议（可选）
- 将近战从“调试线框”升级为正式攻击表现（动画事件 + 命中特效 + hit stop）。
- 增加敌人受击反馈（硬直、击退、受击无敌帧）。
- 将输入切换到可重绑定的 `Input Actions` 资产。
- 为核心模块补最小 PlayMode 测试（移动、资源、施法、近战）。
