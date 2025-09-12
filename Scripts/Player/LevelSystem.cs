using System;
using System.Collections.Generic;
using UnityEngine;
using Xianxia.PlayerDataSystem;

namespace Xianxia.Progression
{
    [Serializable]
    public class RealmConfig
    {
        public string realmId;               // ví dụ: truc_co
        public int levelCap;                 // giới hạn level trong realm này (vd 10)
        public AnimationCurve expCurve;      // đường cong exp (x = level, y = base exp required)
        public int baseExp = 100;            // fallback nếu không có curve
    }

    [CreateAssetMenu(menuName = "Xianxia/LevelConfig", fileName = "LevelConfig")]
    public class LevelConfigSO : ScriptableObject
    {
        public List<RealmConfig> realms = new List<RealmConfig>();

        public RealmConfig GetRealm(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return realms.Find(r => string.Equals(r.realmId, id, StringComparison.OrdinalIgnoreCase));
        }

        public int GetExpRequired(string realmId, int level)
        {
            var rc = GetRealm(realmId);
            if (rc == null) return 0;
            if (rc.expCurve != null && rc.expCurve.length > 0)
            {
                float eval = rc.expCurve.Evaluate(level);
                if (eval < 1f) eval = 1f;
                return Mathf.RoundToInt(eval * rc.baseExp);
            }
            return rc.baseExp * Mathf.Max(1, level);
        }

        public int GetLevelCap(string realmId)
        {
            return GetRealm(realmId)?.levelCap ?? int.MaxValue;
        }
    }

    public class LevelSystem : MonoBehaviour
    {
        [SerializeField] private LevelConfigSO config; // gán trong inspector
        public event Action<int> OnLevelUp;
        public event Action<string> OnRealmBreakthrough; // realm mới

        public bool CanLevelUp(PlayerData data)
        {
            if (data == null || data.stats == null) return false;
            int cap = config != null ? config.GetLevelCap(data.realm) : 10; // default 10 nếu thiếu config
            if (data.level >= cap) return false; // đạt trần realm hiện tại
            int need = GetExpRequired(data.realm, data.level);
            return data.stats.xp >= need;
        }

        public int GetExpRequired(string realm, int level)
        {
            if (config == null) return 100 * Mathf.Max(1, level);
            return config.GetExpRequired(realm, level);
        }

        public bool TryLevelUp(PlayerData data, bool autoBreakthrough = false)
        {
            if (!CanLevelUp(data)) return false;
            int need = GetExpRequired(data.realm, data.level);
            if (data.stats.xp < need) return false;

            data.stats.xp -= need;
            data.level++;
            // cập nhật xpMax theo level mới
            data.stats.xpMax = GetExpRequired(data.realm, data.level);
            OnLevelUp?.Invoke(data.level);
            // nếu vừa vượt khỏi realm cap cũ cần breakthrough tiếp
            if (config != null)
            {
                int cap = config.GetLevelCap(data.realm);
                if (data.level >= cap && autoBreakthrough)
                {
                    // chừa logic breakthrough thao tác bên ngoài
                    // ở đây chỉ thông báo
                }
            }
            PlayerManager.Instance?.SavePlayer();
            return true;
        }

        public bool CanBreakthrough(PlayerData data, string nextRealmId, Func<PlayerData, bool> extraCondition = null)
        {
            if (data == null) return false;
            // ví dụ: phải đang ở trần realm hiện tại
            int currentCap = config != null ? config.GetLevelCap(data.realm) : 10;
            if (data.level < currentCap) return false;
            if (extraCondition != null && !extraCondition(data)) return false;
            var next = config != null ? config.GetRealm(nextRealmId) : null;
            return next != null; // có realm kế
        }

        public bool DoBreakthrough(PlayerData data, string nextRealmId, Func<PlayerData, bool> extraCondition = null)
        {
            if (!CanBreakthrough(data, nextRealmId, extraCondition)) return false;
            data.realm = nextRealmId;
            // reset xp / đặt xpMax cho level hiện tại (hoặc có thể tăng level =1 của realm mới)
            data.stats.xp = 0;
            data.stats.xpMax = GetExpRequired(data.realm, data.level);
            OnRealmBreakthrough?.Invoke(nextRealmId);
            PlayerManager.Instance?.SavePlayer();
            return true;
        }

        public void AddExp(PlayerData data, int amount, bool tryAutoLevel = true)
        {
            if (data == null || data.stats == null || amount <= 0) return;
            data.stats.xp += amount;
            if (data.stats.xpMax <= 0)
                data.stats.xpMax = GetExpRequired(data.realm, data.level);
            if (tryAutoLevel)
            {
                while (CanLevelUp(data))
                {
                    if (!TryLevelUp(data)) break; // an toàn
                }
            }
            PlayerManager.Instance?.SavePlayer();
        }
    }
}
