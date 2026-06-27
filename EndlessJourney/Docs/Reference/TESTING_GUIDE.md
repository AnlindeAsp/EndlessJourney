# Endless Journey Testing Guide

更新时间：2026-06-28  
用途：提供当前核心功能的手动验证方法。自动 PlayMode 测试尚未建立，所以 M0 以手动 smoke test 为主。

## 通用测试规则

- 测试场景：`Assets/Scenes/TestingGround.unity`
- Unity 版本：`6000.3.12f1`
- 每轮测试前清空 Console。
- 所有 Unity Inspector 绑定项都标记为 `Needs Unity Verification`，静态仓库检查不能替代 Playtest。
- 测试结果应回写 `../Planning/FEATURE_TRACKER.md` 和 `../Planning/RISK_REGISTER.md`。

## 0. 项目启动检查

Feature ID：

- TEST-BASELINE-001

步骤：

1. 打开 Unity Hub。
2. 用 Unity `6000.3.12f1` 打开项目。
3. 打开 `Assets/Scenes/TestingGround.unity`。
4. 等待编译完成。
5. 清空 Console。
6. 进入 Play Mode。

通过标准：

- 没有阻断性 Console error。
- TestingGround 可以进入 Play。
- Player 可以响应输入。

记录：

- Needs Playtest。

## 1. Build Settings 检查

Feature ID：

- TEST-BASELINE-001

步骤：

1. 打开 Unity Build Settings。
2. 检查 Scene list。
3. 确认 `Assets/Scenes/TestingGround.unity` 是否存在并启用。

通过标准：

- 如果准备 gameplay build，TestingGround 应启用。
- 如果暂不 build，需要在 `RISK_REGISTER.md` 保留说明。

记录：

- Needs Unity Verification。

## 2. Player Prefab 一致性检查

Feature ID：

- PLAYER-MOVE-001
- COMBAT-RUNTIME-001
- PLAYER-INVINC-001

步骤：

1. 在 TestingGround 选择当前 Player。
2. 记录核心组件是否存在：
   - `PlayerCombatRuntime2D`
   - `WeaponInscriptionEquipped2D`
   - `PlayerInvincibilityCollision2D`
   - `PlayerArmor2D`
   - `ArmorEquipped2D`
   - `PlayerArmorEquipmentSystem2D`
   - `SpellBook2D`
   - `WeaponEquipped2D`
3. 打开 `Assets/prefab/player 1.prefab`。
4. 对比组件和关键引用。

通过标准：

- Prefab 与场景测试 Player 的核心行为一致，或明确记录例外。

记录：

- Needs Unity Verification。

## 3. 玩家移动 Smoke Test

Feature ID：

- PLAYER-MOVE-001
- PLAYER-ABILITY-001

步骤：

1. Play Mode 下按 `A/D` 左右移动。
2. 按跳跃键测试基础跳跃。
3. 在平台边缘测试 coyote time。
4. 提前按跳跃测试 jump buffer。
5. 按 dash 键测试冲刺。
6. 空中测试 dash 限制。
7. 测试二段跳。

通过标准：

- 移动、跳跃、冲刺、二段跳正常。
- 没有异常速度残留。
- 受击后 dash 可被打断。

## 4. 玩家近战 Smoke Test

Feature ID：

- COMBAT-MELEE-001
- COMBAT-DAMAGE-001
- ENEMY-HIT-001

步骤：

1. 靠近敌人。
2. 按攻击键测试横向攻击。
3. 按上方向 + 攻击测试向上攻击。
4. 空中按下方向 + 攻击测试下劈。
5. 命中敌人后观察伤害 log、敌人击退、硬直。
6. 下劈命中后立刻测试 dash / double jump 是否恢复。

通过标准：

- 三方向攻击命中区域符合预期。
- hit context 到达 `EnemyHittable`。
- 敌人扣血、硬直、击退。
- 下劈奖励触发。

记录：

- Needs Playtest。

## 5. 法术 Smoke Test

Feature ID：

- SPELL-BOOK-001
- SPELL-CAST-001
- SPELL-PROJECTILE-001

步骤：

1. 打开 Storage -> Spell 页面。
2. 选择一个已解锁法术。
3. 写入一个法术页。
4. 关闭 Storage。
5. 按对应槽位键释放法术。
6. 对敌人释放 projectile 法术。
7. 测试治疗法术。
8. 观察 mana 消耗、cooldown、cast lock。

通过标准：

- 写入状态保存。
- 槽位施法成功。
- projectile 正常生成、移动、命中或销毁。
- mana 消耗正常。

记录：

- Needs Playtest。

## 6. 武器和 Dual Wielding 测试

Feature ID：

- WEAPON-EQUIP-001
- WEAPON-DUAL-001

步骤：

1. 打开 Storage -> Weapon 页面。
2. 选择已解锁武器。
3. 按 Space 或点击装备。
4. 观察已装备武器高亮。
5. 如果 Dual Wielding 已解锁，切换 dual wielding。
6. 关闭 Storage 后攻击敌人。
7. 观察 hit count、伤害、动画是否变化。

通过标准：

- 当前装备保存到 record.json。
- CombatCore 数值更新。
- Dual Wielding 只在符合条件时可切换。

记录：

- Needs Playtest。

## 7. 铭文 / Forge 测试

Feature ID：

- WEAPON-INSCRIPTION-001
- UI-FORGE-001

步骤：

1. 靠近 Forge 交互点。
2. 按 R 打开 Forge。
3. 选择当前武器。
4. 选择铭文。
5. 按 Space 篆刻。
6. 关闭 Forge。
7. 立刻攻击敌人观察伤害、攻击速度或命中回蓝变化。
8. 重进 Play 或重载 JSON 验证铭文保存。

通过标准：

- 铭文写入 record.json。
- 当前装备武器的铭文即时影响战斗。
- 擦除功能如保留，应能正确清空。

记录：

- Needs Playtest。

## 8. Enemy Perception / Chase 测试

Feature ID：

- ENEMY-AI-001

步骤：

1. 让 Player 在敌人感知范围外。
2. 进入敌人感知范围。
3. 观察 debug indicator 或黑板状态。
4. 离开原位置，观察敌人是否追击。
5. 远离敌人或打断敌人，观察 Return/Patrol 状态。

通过标准：

- 敌人能发现 Player。
- 敌人追击 Player。
- 失去条件下行为符合 FSM 设定。

记录：

- Needs Playtest。

## 9. Armor Equipment / Forge Repair 测试

Feature ID：

- ARMOR-EQUIP-001
- PLAYER-RESOURCE-001
- UI-FORGE-001
- SAVE-DATA-001

前置检查：

- 已创建至少一个 `ArmorData` asset。
- `ArmorLibrary2D -> All Armors` 已注册当前测试护甲。
- `ArmorLibrary2D -> Initial Unlock State` 或 record.json 已解锁测试护甲。
- Player 上已手动绑定 `ArmorEquipped2D -> ArmorLibrary2D`。
- Player 上已手动绑定 `PlayerArmorEquipmentSystem2D -> ArmorEquipped2D / PlayerArmor2D`。
- Forge 交互点如需打开时修理护甲，应手动绑定 `OpenForge2D -> Armor Equipment System`。

步骤：

1. 进入 Play Mode。
2. 通过调试、UI 临时按钮或 Inspector 调用 `ArmorEquipped2D.EquipArmor(armorId)`。
3. 确认 `PlayerArmor2D.MaxDurability` 和 `DamageReductionEfficiency` 应用为 ArmorData 数值。
4. 让 Player 受 harm，观察盔甲耐久按减免伤害下降。
5. 打开 Forge。
6. 如果 `repairArmorOnOpen` 已启用且引用已绑定，确认当前护甲恢复满耐久。
7. 停止 Play 后检查 record.json，再次进入 Play，确认 `equippedArmorId` 可读取。

通过标准：

- 当前护甲 id 写入 record.json。
- ArmorData 的最大耐久和减伤比例应用到 `PlayerArmor2D`。
- Forge 打开时的修理只在手动绑定 armor equipment system 后生效。
- 普通存档点不修复护甲。

记录：

- Needs Unity Verification。
- Needs Playtest。

## 10. Enemy Active Melee 测试

Feature ID：

- ENEMY-ATTACK-001

前置检查：

- `EnemyBrainFSM2D -> Melee Attack Module` 已绑定。
- `EnemyMeleeAttack2D -> Target Layers` 设为 `PlayerSide`。
- contact damage 是否启用已按设计确认。

步骤：

1. 让敌人发现 Player。
2. 保持在攻击检测范围内。
3. 观察 FSM 进入 Attack。
4. 观察 enemy windup / active / recovery / cooldown。
5. 在 windup 中离开范围，确认已起手攻击仍继续。
6. active 窗口触碰 Player，观察玩家 harm。

通过标准：

- FSM 调用 `EnemyMeleeAttack2D.TryStartAttack()`。
- 只在 active 窗口造成伤害。
- 不因 target layer 误伤 interaction/item/player attack trigger。

记录：

- Needs Unity Verification。
- Needs Playtest。

## 11. Contact Damage 冲突测试

Feature ID：

- ENEMY-ATTACK-001
- PLAYER-RESOURCE-001

步骤：

1. 找到同时带 `EnemyContactAttack2D` 和 `EnemyMeleeAttack2D` 的敌人。
2. 进入接触范围。
3. 记录玩家一次近身交互中受到的伤害次数。
4. 确认 contact damage 常驻时，玩家受伤无敌能够避免 active/contact 共存造成失控连伤。

通过标准：

- Contact damage 理论上一直存在。
- Active melee 与 contact damage 可以共存。
- 玩家受伤无敌应避免一次近身交互中出现非预期重复伤害。

记录：

- Needs Playtest。

## 12. 玩家受击、盔甲、无敌、死亡测试

Feature ID：

- PLAYER-RESOURCE-001
- PLAYER-INVINC-001

步骤：

1. 让敌人攻击 Player。
2. 观察生命减少。
3. 观察盔甲耐久减少。
4. 再次立即受击，确认无敌窗口是否阻止 harm。
5. 观察 Player 与 Enemy 是否临时忽略碰撞。
6. 等无敌结束后确认碰撞恢复。
7. 将生命降至 0，观察死亡状态。

通过标准：

- 盔甲按规则减伤。
- 无敌时间生效。
- collision ignore 会恢复。
- 死亡状态触发。

记录：

- Needs Playtest。

## 13. JSON 读写测试

Feature ID：

- SAVE-DATA-001
- SPELL-BOOK-001
- WEAPON-EQUIP-001
- ARMOR-EQUIP-001
- WEAPON-INSCRIPTION-001
- PLAYER-ABILITY-001

步骤：

1. 找到 `Application.persistentDataPath` 下的 `record.json` 和 `PlayerData.json`。
2. 备份或删除当前文件。
3. 进入 Play，触发默认数据生成。
4. 修改武器、护甲、法术页、铭文、能力。
5. 停止 Play，再进入 Play。
6. 检查状态是否恢复。

通过标准：

- record.json 包含 weapon/armor/spell/unlock/equipped/inscription 状态。
- PlayerData.json 包含 abilities、SpellSlotNum、CharmSlotNum。
- 状态可读取恢复。

记录：

- Needs Playtest。

## 14. Library 注册检查

Feature ID：

- WEAPON-EQUIP-001
- ARMOR-EQUIP-001
- WEAPON-INSCRIPTION-001
- SPELL-CAST-001

步骤：

1. 在 TestingGround 找到 `PlayerLibraries`。
2. 检查 `WeaponLibrary2D`。
3. 检查 `SpellLibrary2D`。
4. 检查 `WeaponInscriptionLibrary2D`。
5. 检查 `ArmorLibrary2D`。
6. 对照以下资产目录：
   - `Assets/prefab/WeaponLab`
   - `Assets/prefab/spellLibrarys`
   - `Assets/prefab/WeaponInsLab`
   - 当前 ArmorData asset 所在目录

当前静态发现：

- `Only.asset` 没有被 TestingGround 引用。
- `Will.asset` 没有被 TestingGround 引用。
- spell effect assets 不直接注册在 scene library 是合理的，因为它们由 SpellData 引用。

通过标准：

- 所有应该在当前 UI 出现的资产都已注册。
- 不出现的资产有设计说明。

记录：

- Needs Unity Verification。
- Needs Design Confirmation。

## 15. Console 错误检查

Feature ID：

- TEST-BASELINE-001

步骤：

1. 清空 Console。
2. 完整执行 M0 smoke tests。
3. 停止 Play。
4. 记录 Error 和 Warning。

通过标准：

- 没有阻断性 Error。
- Warning 如果来自已知 Inspector serialized nested fields，应登记但不阻塞。
- 未知 Warning 进入 `RISK_REGISTER.md`。

## M0 测试完成记录模板

```md
日期：
Unity 版本：
场景：
执行人：

通过：
- [ ] 项目启动
- [ ] Player Prefab 一致性
- [ ] 玩家移动
- [ ] 玩家近战
- [ ] 法术
- [ ] 武器
- [ ] 铭文 / Forge
- [ ] Armor equipment / Forge repair
- [ ] 敌人索敌追击
- [ ] 敌人主动近战
- [ ] contact damage 冲突
- [ ] 玩家受击/死亡
- [ ] JSON 读写
- [ ] Library 注册
- [ ] Console 检查

Console Errors：

Console Warnings：

需要更新的 Feature：

需要更新的 Risk：
```
