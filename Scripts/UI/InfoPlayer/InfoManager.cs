using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;
using Xianxia.Stats;

namespace Xianxia.UI.InfoPlayer
{
	public class InfoManager : MonoBehaviour
	{
		[Header("Refs UI")]
		[SerializeField] private TMP_Text playerNameText;
		[SerializeField] private TMP_Text availablePointsText; // hiển thị điểm còn lại (pors hoặc điểm tạm)
		[SerializeField] private TMP_Text levelText;            // hiển thị Level hiện tại
		[SerializeField] private TMP_Text expText;              // hiển thị EXP: current / required (percent)
		[SerializeField] private TMP_Text realmText;            // hiển thị Realm hiện tại
		[SerializeField] private Button toggleAllocateButton;  // nút "Cộng điểm" bật/tắt chế độ phân phối
		[SerializeField] private Button saveButton;
		[SerializeField] private Button cancelButton;
		[SerializeField] private Transform rowsParent;         // parent chứa các row stat
		[SerializeField] private StatAllocateRow rowPrefab;

		[Header("Equipped Items Display")] // khu vực chỉ hiển thị trang bị đang mặc
		[SerializeField] private Transform equippedItemsParent; // content riêng
		[SerializeField] private EquippedItemView equippedItemPrefab; // prefab có Image icon, TMP name, TMP desc
		[SerializeField] private ItemDatabaseSO itemDatabase;
		[Tooltip("Các slot id không muốn hiển thị (vd: body, underwear, internal)" )]
		[SerializeField] private List<string> hiddenSlotIds = new List<string>();

		[Header("Stat Config")] // danh sách stat muốn hiển thị (id -> label)
		[SerializeField] private List<StatEntry> statsToShow = new List<StatEntry>
		{
			new StatEntry("hpMax", "Sinh lực"),
			new StatEntry("qiMax", "Chân khí"),
			new StatEntry("atk", "Tấn công"),
			new StatEntry("def", "Phòng thủ"),
			new StatEntry("critRate", "Chí mạng"),
			new StatEntry("critDmg", "Sát thương CM"),
			new StatEntry("moveSpd", "Tốc độ"),
			new StatEntry("hpRegen", "Hồi HP"),
			new StatEntry("qiRegen", "Hồi Khí"),
			new StatEntry("lifesteal", "Hút máu"),
			new StatEntry("spellPower", "Phép"),
			new StatEntry("spellResist", "Kháng phép"),
			new StatEntry("dodge", "Né"),
			new StatEntry("pierce", "Xuyên")
		};

		[System.Serializable]
		public class StatEntry
		{
			public string id;
			public string label;
			public StatEntry(string id, string label){ this.id = id; this.label = label; }
		}

		private readonly List<StatAllocateRow> _rows = new List<StatAllocateRow>();
		private PlayerStatsManager _statsManager;
		private PlayerData _playerData;
		private StatCollection _statCollection; // cached ref to data.stats
		private readonly List<EquippedItemView> _equipViews = new List<EquippedItemView>();

		private int _available;       // điểm thực tế có (pors)
		private int _tempRemaining;   // điểm còn lại trong phiên phân phối
		private bool _allocating = false;

		private void Awake()
		{
			_statsManager = UnityEngine.Object.FindFirstObjectByType<PlayerStatsManager>();
			_playerData = PlayerManager.Instance?.Data;
			_statCollection = _playerData?.stats;
			BuildRows();
			BuildEquippedViews();
			Hook();
			RefreshAll();
			SetAllocateMode(false);
		}

		private void OnEnable()
		{
			if (PlayerManager.Instance != null)
			{
				PlayerManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
			}
			// Subscribe level / stats events nếu có
			if (_statsManager != null)
				_statsManager.onStatsRecalculated.AddListener(OnStatsRecalculated);
		}
		private void OnDisable()
		{
			if (PlayerManager.Instance != null)
			{
				PlayerManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
			}
			if (_statsManager != null)
				_statsManager.onStatsRecalculated.RemoveListener(OnStatsRecalculated);
		}

		private void OnPlayerDataLoaded(PlayerData data)
		{
			_playerData = data;
			_statsManager = UnityEngine.Object.FindFirstObjectByType<PlayerStatsManager>();
			_statCollection = _playerData?.stats;
			RefreshAll();
			RefreshEquippedItems();
			RefreshRealm();
		}

		private void OnStatsRecalculated()
		{
			RefreshLevelExpSection();
		}

		// LevelUp no longer handled here (Tu Vi based progression)

		private void Hook()
		{
			if (toggleAllocateButton) toggleAllocateButton.onClick.AddListener(ToggleAllocateMode);
			if (saveButton) saveButton.onClick.AddListener(OnClickSave);
			if (cancelButton) cancelButton.onClick.AddListener(OnClickCancel);
		}

		private void BuildRows()
		{
			if (rowsParent == null || rowPrefab == null) return;
			// Chỉ xóa các child có component StatAllocateRow (giữ Name, Points... chung parent)
			for (int i = rowsParent.childCount - 1; i >= 0; i--)
			{
				var child = rowsParent.GetChild(i);
				if (child.GetComponent<StatAllocateRow>() != null)
				{
					Destroy(child.gameObject);
				}
			}
			_rows.Clear();
			foreach (var entry in statsToShow)
			{
				var row = Instantiate(rowPrefab, rowsParent);
				row.Init(this, entry.id, entry.label);
				_rows.Add(row);
			}
		}

		private void RefreshAll()
		{
			// Có thể Awake chạy trước khi PlayerManager khởi tạo / load data => cần guard null
			if (_playerData == null || _playerData.stats == null)
			{
				_available = 0;
				if (!_allocating) _tempRemaining = 0;
				RefreshAvailablePointsUI();
				_statCollection = null;
				foreach (var r in _rows) r.RefreshValue(null);
				UpdateButtonsState();
				return; // Chờ OnPlayerDataLoaded gọi lại
			}

			if (playerNameText) playerNameText.text = _playerData.name;

			_available = Mathf.RoundToInt(_playerData.stats.GetBase(StatId.Points));
			if (!_allocating) _tempRemaining = _available;
			RefreshAvailablePointsUI();

			_statCollection = _playerData.stats;
			foreach (var r in _rows) r.RefreshValue(_statCollection);
			UpdateButtonsState();
			RefreshEquippedItems();
			RefreshLevelExpSection();
			RefreshRealm();
		}

		private void RefreshLevelExpSection()
		{
			if (_playerData == null || _playerData.stats == null) return;
			int level = _playerData.level;
			float tuVi = _playerData.stats.GetFinal(StatId.TuVi);
			float can = Mathf.Max(1f, _playerData.stats.GetFinal(StatId.TuViCan));
			float pct = Mathf.Clamp01(tuVi / can);
			if (levelText) levelText.text = $"Tầng {level}"; // hoặc đổi label nếu cần
			if (expText) expText.text = $"Tu Vi: {tuVi:0}/{can:0} ({pct * 100f:0.0}%)";
			RefreshRealm();
		}

		private void RefreshRealm()
		{
			if (realmText == null || _playerData == null) return;
			var realmEnum = _playerData.realm;
			var (label, color) = GetRealmDisplay(realmEnum);
			realmText.text = label;
			realmText.color = color;
		}

		// Public API để ép cập nhật lại dữ liệu mới nhất mỗi khi tab mở
		public void RefreshNow()
		{
			// lấy lại tham chiếu data phòng trường hợp PlayerManager đổi instance
			_playerData = PlayerManager.Instance?.Data;
			_statsManager = UnityEngine.Object.FindFirstObjectByType<PlayerStatsManager>();
			RefreshAll();
			// Ép rebuild layout để Text / ContentSizeFitter cập nhật đúng kích thước khi panel vừa mở
			ForceLayoutRebuild();
		}

		private void ForceLayoutRebuild()
		{
			// Thực hiện 2 frame: rebuild ngay và queue thêm 1 late rebuild nếu cần
			if (!gameObject.activeInHierarchy) return; // panel chưa active => bỏ
			var root = transform as RectTransform;
			if (root == null) return;
			Canvas.ForceUpdateCanvases();
			UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(root);
			// Schedule 1 more at end of frame để chắc chắn (trong trường hợp icon async làm thay đổi chiều cao)
			StartCoroutine(CoDelayedRebuild(root));
		}

		private System.Collections.IEnumerator CoDelayedRebuild(RectTransform root)
		{
			yield return null; // chờ 1 frame
			if (root == null || !root.gameObject.activeInHierarchy) yield break;
			Canvas.ForceUpdateCanvases();
			UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(root);
		}

		private bool IsHiddenSlot(string slotId)
		{
			if (string.IsNullOrEmpty(slotId) || hiddenSlotIds == null) return false;
			for (int i = 0; i < hiddenSlotIds.Count; i++)
			{
				if (string.Equals(hiddenSlotIds[i], slotId, System.StringComparison.OrdinalIgnoreCase)) return true;
			}
			return false;
		}

		private void BuildEquippedViews()
		{
			if (equippedItemsParent == null || equippedItemPrefab == null) return;
			// Xóa mạnh mẽ tất cả con (tránh case Destroy delay gây nhân đôi ở frame kế tiếp)
			for (int i = equippedItemsParent.childCount - 1; i >= 0; i--)
			{
				var child = equippedItemsParent.GetChild(i).gameObject;
#if UNITY_EDITOR
				if (!Application.isPlaying)
					DestroyImmediate(child);
				else
#endif
					Destroy(child);
			}
			_equipViews.Clear();
			if (_playerData?.equipment == null) return;
			var usedSlots = new HashSet<string>();
			foreach (var (slotId, item) in _playerData.equipment.EnumerateSlots())
			{
				if (IsHiddenSlot(slotId)) continue;
				if (string.IsNullOrEmpty(slotId)) continue;
				if (IsSlotEmpty(item)) continue; // slot trống theo định nghĩa mới
				if (!usedSlots.Add(slotId)) continue; // tránh trùng
				var view = Instantiate(equippedItemPrefab, equippedItemsParent);
				UpdateEquippedViewAsync(view, slotId, item);
				_equipViews.Add(view);
			}
		}

		private void RefreshEquippedItems()
		{
			if (equippedItemsParent == null || equippedItemPrefab == null) return;
			if (_playerData?.equipment == null) return;
			// Luôn rebuild danh sách gọn chỉ gồm item thực sự có
			BuildEquippedViews();
			ForceLayoutRebuild();
		}

		private async void UpdateEquippedViewAsync(EquippedItemView view, string slotId, InventoryItem item)
		{
			if (view == null || IsSlotEmpty(item)) return;
			Sprite icon = null;
			var db = itemDatabase != null ? itemDatabase : ItemDatabaseSO.Instance;
			var def = db != null ? db.GetById(item.id) : null;
			string iconAddr = def != null && !string.IsNullOrEmpty(def.addressIcon) ? def.addressIcon : item.addressIcon;
			if (!string.IsNullOrEmpty(iconAddr))
			{
				icon = await Xianxia.Items.ItemAssets.LoadIconSpriteAsync(iconAddr);
			}
			string name = item.name ?? item.id;
			string desc = InfoItem.BuildDescription(item, db);
			view.SetData(slotId, icon, name, desc);
			// Mỗi lần icon loaded có thể thay đổi kích thước layout
			ForceLayoutRebuild();
		}

		private bool IsSlotEmpty(InventoryItem item)
		{
			return item == null || string.IsNullOrEmpty(item.id);
		}

		// Removed GetCurrentStats (legacy PlayerStats)

		private void RefreshAvailablePointsUI()
		{
			if (availablePointsText)
			{
				const string colorHex = "#FF3A3A"; // đỏ nhẹ dễ nhìn
				if (_allocating)
					availablePointsText.text = $"Điểm còn lại: <color={colorHex}>{_tempRemaining}</color>";
				else
					availablePointsText.text = $"Điểm tu luyện: <color={colorHex}>{_available}</color>";
			}
		}

		private void ToggleAllocateMode()
		{
			SetAllocateMode(!_allocating);
		}

		private void SetAllocateMode(bool enable)
		{
			_allocating = enable;
			_tempRemaining = enable ? _available : _available; // reset khi bật
			foreach (var r in _rows)
			{
				if (enable) r.ResetPending();
				r.SetAllocateMode(enable);
			}
			// Ẩn nút bật chế độ khi đang phân phối, hiện lại khi thoát
			if (toggleAllocateButton) toggleAllocateButton.gameObject.SetActive(!enable);
			RefreshAll();
		}

		public bool CanSpendPoint() => _allocating && _tempRemaining > 0;
		public void SpendTempPoint() { if (_tempRemaining > 0) _tempRemaining--; RefreshAvailablePointsUI(); }
		public void RefundTempPoint() { _tempRemaining++; RefreshAvailablePointsUI(); }

		private void OnClickSave()
		{
			if (!_allocating || _playerData == null) return;
			int used = _available - _tempRemaining;
			if (used <= 0) { SetAllocateMode(false); return; }

			// Áp dụng các pending
			foreach (var r in _rows)
			{
				int add = r.ConsumePending(out string id);
				if (add <= 0) continue;
				ApplyStatIncrease(_playerData.stats, id, add);
			}

			// Cập nhật lại số điểm còn lại về base Points
			_playerData.stats.SetBase(StatId.Points, _tempRemaining);
			// Recalc sau khi thay đổi cap (hpMax, qiMax ...)
			_statsManager?.RecalculateAll(_playerData);
			PlayerManager.Instance?.SavePlayer();
			SetAllocateMode(false);
		}

		private void OnClickCancel()
		{
			if (!_allocating) return;
			SetAllocateMode(false);
		}

		private void ApplyStatIncrease(StatCollection stats, string id, int add)
		{
			if (stats == null || add <= 0) return;
			Xianxia.UI.StatUiMapper.AllocatePoints(stats, id, add);
		}

		private void UpdateButtonsState()
		{
			if (saveButton) saveButton.gameObject.SetActive(_allocating);
			if (cancelButton) cancelButton.gameObject.SetActive(_allocating);
		}

		private (string label, Color color) GetRealmDisplay(Xianxia.Items.Realm realm)
		{
			switch (realm)
			{
				case Xianxia.Items.Realm.PhamNhan:
					return ("Phàm nhân", Color.white);
				case Xianxia.Items.Realm.luyen_khi:
					return ("Luyện khí", new Color(0.65f, 0.90f, 1f)); // light cyan
				case Xianxia.Items.Realm.truc_co:
					return ("Trúc cơ", new Color(0.55f, 1f, 0.55f)); // greenish
				case Xianxia.Items.Realm.kim_dan:
					return ("Kim đan", new Color(1f, 0.85f, 0.35f)); // golden
				case Xianxia.Items.Realm.nguyen_anh:
					return ("Nguyên anh", new Color(1f, 0.55f, 0.35f)); // orange
				case Xianxia.Items.Realm.hoa_than:
					return ("Hóa thần", new Color(0.9f, 0.4f, 0.9f)); // purple
				case Xianxia.Items.Realm.luyen_hu:
					return ("Luyện hư", new Color(0.6f, 0.4f, 1f)); // violet
				case Xianxia.Items.Realm.hop_the:
					return ("Hợp thể", new Color(0.3f, 0.8f, 1f)); // azure
				case Xianxia.Items.Realm.dai_thua:
					return ("Đại thừa", new Color(1f, 0.3f, 0.3f)); // red
				case Xianxia.Items.Realm.chuan_tien:
					return ("Độ kiếp", new Color(1f, 1f, 0.6f)); // pale yellow
				default:
					return (realm.ToString(), Color.white);
			}
		}
	}
}
