# Endless Journey Risk Register

更新时间：2026-06-28  
用途：记录技术风险、配置风险、阻塞项和解决方案。旧 `problem.md` 的有效内容已迁移到这里。

## Risk Table

| ID | 风险 | 严重性 | 影响功能 | 是否阻塞 | 当前状态 | 解决方案 | 下一步 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| RISK-001 | TestingGround Player 与 `Assets/prefab/player 1.prefab` 漂移 | Medium | PLAYER-MOVE-001, COMBAT-RUNTIME-001, PLAYER-INVINC-001 | No for current TestingGround, yes for future prefab reuse | 2026-06-28 用户确认当前 M0 操作可用；Prefab 复现仍是后续工程风险 | M1/M2 前决定同步 Prefab，或明确 TestingGround scene instance 为当前基线 | M1/M2 |
| RISK-002 | Build Settings 仍只包含 `SampleScene.unity` | Low | TEST-BASELINE-001 | No for editor play, yes for build | 2026-06-28 用户确认当前 M0 操作可用；Build Settings 对当前 editor 测试不阻塞 | 需要打包或正式测试入口时再处理 Build Settings | Later |
| RISK-003 | `EnemyMeleeAttack2D` 未接入 FSM | Low | ENEMY-ATTACK-001 | No for current M0 | 2026-06-28 用户确认 M0 enemy 操作可用 | 后续 M4 正式敌人切片继续验证 | M4 |
| RISK-004 | Enemy active melee target layer 过宽 | Low | ENEMY-ATTACK-001, PLAYER-INVINC-001 | No for current M0 | 2026-06-28 用户确认 M0 enemy/player harm 行为可用，未反馈误伤阻断问题 | 正式敌人 prefab 化时继续检查 target layer | M4 |
| RISK-005 | Active attack 与 contact damage 共存需要 Playtest | Low | ENEMY-ATTACK-001, PLAYER-RESOURCE-001 | No for current M0 | 2026-06-28 用户确认 M0 操作逻辑可用；contact + active 共存未反馈失控连伤 | 正式敌人类型继续 Playtest | M4 |
| RISK-006 | Spawner 生成的敌人可能不符合测试目标 | Low | ENEMY-SPAWN-001, ENEMY-AI-001 | No for current M0 | 2026-06-28 用户确认 M0 操作可用 | 地图与怪物切片阶段重新确认 spawner prefab | M4 |
| RISK-007 | `Only.asset` 未进入当前 Weapon Library | Low | WEAPON-EQUIP-001 | No | 设计确认：`Only.asset` 是拓展特殊组合武器，暂不放入当前游戏流程；M0 当前可用 | 后续扩展 Only/Will 组合时再接入 | Later |
| RISK-008 | `Will.asset` 未进入当前 Inscription Library | Low | WEAPON-INSCRIPTION-001 | No | 设计确认：`Will.asset` 是 Only/Will 特殊绑定组合铭文，暂不放入当前游戏流程；M0 当前可用 | 后续扩展绑定/不可卸下规则时再接入 | Later |
| RISK-009 | `beginer_sword` / `beginner_sword` 拼写不一致 | Low | WEAPON-INSCRIPTION-001, SAVE-DATA-001 | No for current M0 | 2026-06-28 用户确认 M0 JSON/装备相关操作可用 | 若后续 record 或 Inspector 出现 id mismatch 再统一 | Watch |
| RISK-010 | Physics2D Collision Matrix 全开 | Low | PLAYER-INVINC-001, ENEMY-ATTACK-001 | No for current M0 | 2026-06-28 用户确认当前 M0 操作可用 | 正式 prefab / layer 规范化时再收紧 Collision Matrix | M1/M4 |
| RISK-011 | PlayerCore action lock 需要组合验证 | Low | UI-CANVAS-001, SPELL-CAST-001, PLAYER-MOVE-001 | No | 2026-06-28 已改为 owner-based lock；仍需 Playtest Storage/Forge/Spell lock 组合 | 在 TestingGround 验证 UI 打开、施法、打断、关闭时不会互相误解锁 | M1 |
| RISK-012 | 玩家动作模块缺少统一取消规则 | Medium | COMBAT-MELEE-001, SPELL-CAST-001, PLAYER-MOVE-001 | No for M0 | dash、harm、spell、melee 目前局部处理 | M1 建动作优先级与取消表 | M1 |
| RISK-013 | Save 数据无版本号/备份/迁移 | Medium | SAVE-DATA-001, SAVE-RESPAWN-001 | No for prototype | `record.json` / `PlayerData.json` 已有字段，但无 version | M3 前加入版本和迁移策略 | M3 |
| RISK-014 | 手动 Inspector 配置难以复现 | Medium | 所有 scene/prefab 集成功能 | No for current TestingGround, yes for future reuse | 2026-06-28 当前 TestingGround M0 操作可用；多个关键引用仍依赖手动接线 | M1/M2 前考虑 prefab baseline 或接线清单 | M1/M2 |
| RISK-015 | 缺少自动 PlayMode 测试 | Medium | TEST-BASELINE-001 | No, but slows regression detection | 未发现 `*Test*.cs` 或测试 asmdef | 先建立 manual smoke test，M1/M2 补最小 PlayMode | M1 |
| RISK-016 | 文档状态重复和冲突 | Medium | 项目协作 | No | 根目录多个文档同时记录状态 | 以 `FEATURE_TRACKER.md` 为唯一状态源，旧文档改为导航/历史 | 本次任务 |
| RISK-017 | Unity Volume Profile 中存在 `m_Script: {fileID: 0}` | Low | Build/Rendering | No, likely package/profile related | `Assets/Settings/DefaultVolumeProfile.asset` 有 missing script-like refs | Unity 中检查 Volume Profile 是否正常 | Needs Unity Verification |
| RISK-018 | Armor 后端已完成但未接入 Unity/UI | Medium | ARMOR-EQUIP-001, PLAYER-RESOURCE-001, UI-FORGE-001, SAVE-DATA-001 | No for code, yes for actual gameplay use | `ArmorData / ArmorLibrary2D / ArmorEquipped2D / PlayerArmorEquipmentSystem2D` 已存在；缺 ArmorData assets、Inspector 绑定和 Forge Armor 页面 | M2 创建资产并手动绑定，先验证 record/apply/repair，再做 Forge Armor UI | M2 |

## Current Build / Static Check Notes

- Unity version: `6000.3.12f1`.
- Main editor test scene: `Assets/Scenes/TestingGround.unity`.
- Build Settings currently enabled scene: `Assets/Scenes/SampleScene.unity`.
- No custom scene/prefab `m_Script: {fileID: 0}` was found in earlier project audit; current static search only found such refs in `DefaultVolumeProfile.asset`.
- No test C# files were found by `rg --files -g "*Test*.cs"`.

## Risk Update Rule

Whenever a risk is resolved:

1. Update `Current Status`.
2. Update related Feature row in `FEATURE_TRACKER.md`.
3. Add a line to `DEVELOPMENT_LOG.md` if it changes project baseline.
4. Remove or downgrade the risk only after Unity verification or Playtest when applicable.
