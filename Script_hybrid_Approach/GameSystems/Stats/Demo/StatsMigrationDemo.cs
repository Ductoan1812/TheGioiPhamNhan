using System.Collections.Generic;
using UnityEngine;
using Foundation.Utils;
using Foundation.Architecture;
using GameSystems.Stats;
using GameSystems.Stats.Core;

namespace GameSystems.Stats.Demo
{
    /// <summary>
    /// StatsMigrationDemo - Demo script để show cách migrate từ StatSystem.cs cũ 
    /// sang StatManager/StatCollection mới.
    /// 
    /// Attach script này vào GameObject và chạy để test migration.
    /// </summary>
    public class StatsMigrationDemo : MonoBehaviour
    {
        [Header("Demo Configuration")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;
        
        [Header("Test Data")]
        [SerializeField] private string testPlayerId = "Player_001";
        [SerializeField] private string testEnemyId = "Enemy_001";
        
        private void Start()
        {
            if (runOnStart)
            {
                StartCoroutine(RunMigrationDemo());
            }
        }
        
        private System.Collections.IEnumerator RunMigrationDemo()
        {
            yield return new WaitForSeconds(0.1f); // Wait for all systems to initialize
            
            Debug.Log("=== STATS MIGRATION DEMO STARTED ===");
            
            // Step 1: Simulate old system data
            DemoOldSystemData();
            yield return new WaitForSeconds(0.5f);
            
            // Step 2: Migrate to new system
            DemoMigrationProcess();
            yield return new WaitForSeconds(0.5f);
            
            // Step 3: Show new features
            DemoNewFeatures();
            yield return new WaitForSeconds(0.5f);
            
            // Step 4: Performance comparison
            DemoPerformanceComparison();
            yield return new WaitForSeconds(0.5f);
            
            Debug.Log("=== STATS MIGRATION DEMO COMPLETED ===");
        }
        
        #region Demo Methods
        
        private void DemoOldSystemData()
        {
            Debug.Log("\n--- 1. SIMULATING OLD SYSTEM DATA ---");
            
            // Simulate legacy PlayerStatsManager data
            var legacyPlayerStats = new Dictionary<StatId, float>
            {
                { StatId.KhiHuyetMax, 1000f },
                { StatId.KhiHuyet, 750f },
                { StatId.LinhLucMax, 500f },
                { StatId.LinhLuc, 300f },
                { StatId.ThoNguyenMax, 200f },
                { StatId.ThoNguyen, 150f },
                { StatId.CongVatLy, 85f },
                { StatId.CongPhapThuat, 120f },
                { StatId.PhongVatLy, 45f },
                { StatId.PhongPhapThuat, 60f },
                { StatId.TocDo, 5.5f },
                { StatId.TiLeBaoKich, 12f },
                { StatId.SatThuongBaoKich, 250f },
                { StatId.InventorySize, 30f }
            };
            
            Debug.Log($"Legacy Player Stats: {legacyPlayerStats.Count} stats");
            foreach (var stat in legacyPlayerStats)
            {
                Debug.Log($"  {stat.Key}: {stat.Value}");
            }
            
            // Simulate enemy data
            var legacyEnemyStats = new Dictionary<StatId, float>
            {
                { StatId.KhiHuyetMax, 800f },
                { StatId.KhiHuyet, 800f },
                { StatId.CongVatLy, 65f },
                { StatId.PhongVatLy, 35f },
                { StatId.TocDo, 4f }
            };
            
            Debug.Log($"Legacy Enemy Stats: {legacyEnemyStats.Count} stats");
        }
        
        private void DemoMigrationProcess()
        {
            Debug.Log("\n--- 2. MIGRATING TO NEW SYSTEM ---");
            
            // Get StatManager instance
            var statManager = StatManager.Instance;
            if (statManager == null)
            {
                Debug.LogError("StatManager not found! Make sure it's in the scene.");
                return;
            }
            
            // Legacy data simulation
            var legacyPlayerStats = new Dictionary<StatId, float>
            {
                { StatId.KhiHuyetMax, 1000f },
                { StatId.KhiHuyet, 750f },
                { StatId.LinhLucMax, 500f },
                { StatId.LinhLuc, 300f },
                { StatId.CongVatLy, 85f },
                { StatId.CongPhapThuat, 120f },
                { StatId.TocDo, 5.5f }
            };
            
            // Step 1: Register player with legacy data
            Debug.Log("Migrating Player stats...");
            var playerStats = statManager.RegisterEntityFromLegacy(testPlayerId, legacyPlayerStats);
            
            if (playerStats != null)
            {
                Debug.Log($"✅ Player migrated successfully!");
                Debug.Log($"   HP: {playerStats.GetStat(StatId.KhiHuyet)}/{playerStats.GetStat(StatId.KhiHuyetMax)}");
                Debug.Log($"   MP: {playerStats.GetStat(StatId.LinhLuc)}/{playerStats.GetStat(StatId.LinhLucMax)}");
                Debug.Log($"   Attack: {playerStats.GetStat(StatId.CongVatLy)} Physical, {playerStats.GetStat(StatId.CongPhapThuat)} Magic");
            }
            
            // Step 2: Register enemy with default stats
            Debug.Log("Creating Enemy stats...");
            var enemyStats = statManager.RegisterEntity(testEnemyId);
            if (enemyStats != null)
            {
                // Customize enemy stats
                enemyStats.SetBaseStat(StatId.KhiHuyetMax, 800f);
                enemyStats.SetBaseStat(StatId.KhiHuyet, 800f);
                enemyStats.SetBaseStat(StatId.CongVatLy, 65f);
                enemyStats.SetBaseStat(StatId.PhongVatLy, 35f);
                enemyStats.SetBaseStat(StatId.TocDo, 4f);
                
                Debug.Log($"✅ Enemy created successfully!");
                Debug.Log($"   HP: {enemyStats.GetStat(StatId.KhiHuyet)}/{enemyStats.GetStat(StatId.KhiHuyetMax)}");
                Debug.Log($"   Attack: {enemyStats.GetStat(StatId.CongVatLy)}");
            }
            
            Debug.Log($"StatManager now manages {statManager.EntityCount} entities");
        }
        
        private void DemoNewFeatures()
        {
            Debug.Log("\n--- 3. DEMONSTRATING NEW FEATURES ---");
            
            var statManager = StatManager.Instance;
            var playerStats = statManager.GetEntityStats(testPlayerId);
            
            if (playerStats == null)
            {
                Debug.LogError("Player stats not found!");
                return;
            }
            
            // Demo 1: Equipment Bonuses
            Debug.Log("🗡️ Equipping Legendary Sword...");
            var swordBonus1 = new StatBonus(StatId.CongVatLy, 50f, BonusType.Flat, "LegendarySword");
            var swordBonus2 = new StatBonus(StatId.TiLeBaoKich, 8f, BonusType.Flat, "LegendarySword");
            
            playerStats.AddBonus(swordBonus1);
            playerStats.AddBonus(swordBonus2);
            
            Debug.Log($"   Attack Power: {playerStats.GetStat(StatId.CongVatLy)} (+{swordBonus1.Value} from sword)");
            Debug.Log($"   Crit Rate: {playerStats.GetStat(StatId.TiLeBaoKich)}% (+{swordBonus2.Value}% from sword)");
            
            // Demo 2: Buff/Debuff System
            Debug.Log("✨ Casting Strength Buff...");
            var strengthBuff = new StatBonus(StatId.CongVatLy, 25f, BonusType.Percentage, "StrengthPotion", 0, 30f);
            playerStats.AddBonus(strengthBuff);
            
            Debug.Log($"   Attack Power: {playerStats.GetStat(StatId.CongVatLy)} (+25% from buff)");
            
            // Demo 3: Level-up Bonuses
            Debug.Log("📈 Level Up! (+5 levels)");
            var levelBonus = new StatBonus(StatId.KhiHuyetMax, 250f, BonusType.Flat, "LevelUp");
            playerStats.AddBonus(levelBonus);
            
            // Restore HP to new max
            playerStats.RestoreResource(StatId.KhiHuyet, StatId.KhiHuyetMax);
            Debug.Log($"   Max HP: {playerStats.GetStat(StatId.KhiHuyetMax)} (+{levelBonus.Value} from levels)");
            Debug.Log($"   Current HP: {playerStats.GetStat(StatId.KhiHuyet)} (restored to max)");
            
            // Demo 4: Multiple Bonus Types
            Debug.Log("🏃‍♂️ Speed Enhancement Stack...");
            var baseSpeedBonus = new StatBonus(StatId.TocDo, 2f, BonusType.Flat, "RuneOfSpeed");
            var multSpeedBonus = new StatBonus(StatId.TocDo, 0.3f, BonusType.Multiplier, "WindBless");
            
            playerStats.AddBonus(baseSpeedBonus);
            playerStats.AddBonus(multSpeedBonus);
            
            Debug.Log($"   Speed: {playerStats.GetStat(StatId.TocDo)} (base + flat + 30% mult)");
            
            // Demo 5: Bonus Removal
            Debug.Log("⏰ Strength Buff Expired...");
            playerStats.RemoveBonus(strengthBuff);
            Debug.Log($"   Attack Power: {playerStats.GetStat(StatId.CongVatLy)} (buff removed)");
            
            // Demo 6: Source-based Removal
            Debug.Log("🗡️ Unequipping Legendary Sword...");
            playerStats.RemoveBonusesFromSource("LegendarySword");
            Debug.Log($"   Attack Power: {playerStats.GetStat(StatId.CongVatLy)} (sword bonuses removed)");
            Debug.Log($"   Crit Rate: {playerStats.GetStat(StatId.TiLeBaoKich)}% (sword bonuses removed)");
        }
        
        private void DemoPerformanceComparison()
        {
            Debug.Log("\n--- 4. PERFORMANCE COMPARISON ---");
            
            var statManager = StatManager.Instance;
            var playerStats = statManager.GetEntityStats(testPlayerId);
            
            if (playerStats == null) return;
            
            // Add many bonuses for performance test
            Debug.Log("Adding 100 random bonuses for performance test...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 100; i++)
            {
                var randomStat = (StatId)(i % System.Enum.GetValues(typeof(StatId)).Length);
                var bonus = new StatBonus(randomStat, Random.Range(1f, 10f), BonusType.Flat, $"PerfTest_{i}");
                playerStats.AddBonus(bonus);
            }
            
            stopwatch.Stop();
            Debug.Log($"✅ Added 100 bonuses in {stopwatch.ElapsedMilliseconds}ms");
            
            // Test stat calculation performance
            stopwatch.Restart();
            
            for (int i = 0; i < 1000; i++)
            {
                _ = playerStats.GetStat(StatId.CongVatLy);
                _ = playerStats.GetStat(StatId.KhiHuyetMax);
                _ = playerStats.GetStat(StatId.TocDo);
            }
            
            stopwatch.Stop();
            Debug.Log($"✅ 3000 stat lookups in {stopwatch.ElapsedMilliseconds}ms");
            
            // Cleanup performance test bonuses
            playerStats.RemoveBonusesFromSource("PerfTest_0");
            for (int i = 1; i < 100; i++)
            {
                playerStats.RemoveBonusesFromSource($"PerfTest_{i}");
            }
            
            Debug.Log("🧹 Cleaned up performance test bonuses");
        }
        
        #endregion
        
        #region Manual Testing Methods
        
        [ContextMenu("Run Migration Demo")]
        public void RunDemoManually()
        {
            StartCoroutine(RunMigrationDemo());
        }
        
        [ContextMenu("Test Combat Simulation")]
        public void TestCombatSimulation()
        {
            Debug.Log("\n--- COMBAT SIMULATION TEST ---");
            
            var statManager = StatManager.Instance;
            if (statManager == null) return;
            
            var playerStats = statManager.GetEntityStats(testPlayerId);
            var enemyStats = statManager.GetEntityStats(testEnemyId);
            
            if (playerStats == null || enemyStats == null)
            {
                Debug.LogError("Player or Enemy stats not found! Run migration demo first.");
                return;
            }
            
            // Simulate combat
            float playerAttack = playerStats.GetStat(StatId.CongVatLy);
            float enemyDefense = enemyStats.GetStat(StatId.PhongVatLy);
            float damage = Mathf.Max(1f, playerAttack - enemyDefense);
            
            Debug.Log($"⚔️ Player attacks Enemy!");
            Debug.Log($"   Player Attack: {playerAttack}");
            Debug.Log($"   Enemy Defense: {enemyDefense}");
            Debug.Log($"   Damage Dealt: {damage}");
            
            // Apply damage
            float actualDamage = statManager.DamageEntity(testEnemyId, damage);
            Debug.Log($"   Enemy HP: {enemyStats.GetStat(StatId.KhiHuyet)}/{enemyStats.GetStat(StatId.KhiHuyetMax)} (-{actualDamage})");
            
            // Heal player
            float healAmount = statManager.HealEntity(testPlayerId, 50f);
            Debug.Log($"🩹 Player healed for {healAmount}");
            Debug.Log($"   Player HP: {playerStats.GetStat(StatId.KhiHuyet)}/{playerStats.GetStat(StatId.KhiHuyetMax)}");
        }
        
        [ContextMenu("Log All Stats")]
        public void LogAllStatsDebug()
        {
            var statManager = StatManager.Instance;
            if (statManager != null)
            {
                statManager.LogAllEntityStats();
            }
        }
        
        [ContextMenu("Validate All Stats")]
        public void ValidateAllStats()
        {
            var statManager = StatManager.Instance;
            if (statManager != null)
            {
                bool isValid = statManager.ValidateAllStats();
                Debug.Log($"Stats Validation: {(isValid ? "✅ PASSED" : "❌ FAILED")}");
            }
        }
        
        #endregion
    }
}
