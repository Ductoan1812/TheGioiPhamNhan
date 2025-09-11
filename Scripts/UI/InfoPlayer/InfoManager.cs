using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Xianxia.PlayerDataSystem;

namespace Xianxia.UI.InfoPlayer
{
	public class InfoManager : MonoBehaviour
	{
		[Header("Refs UI")]
		[SerializeField] private TMP_Text playerNameText;
		[SerializeField] private TMP_Text availablePointsText; // hiển thị điểm còn lại (pors hoặc điểm tạm)
		[SerializeField] private Button toggleAllocateButton;  // nút "Cộng điểm" bật/tắt chế độ phân phối
		[SerializeField] private Button saveButton;
		[SerializeField] private Button cancelButton;
		[SerializeField] private Transform rowsParent;         // parent chứa các row stat
		[SerializeField] private StatAllocateRow rowPrefab;

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

		private int _available;       // điểm thực tế có (pors)
		private int _tempRemaining;   // điểm còn lại trong phiên phân phối
		private bool _allocating = false;

		private void Awake()
		{
			_statsManager = Object.FindFirstObjectByType<PlayerStatsManager>();
			_playerData = PlayerManager.Instance?.Data;
			BuildRows();
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
		}
		private void OnDisable()
		{
			if (PlayerManager.Instance != null)
			{
				PlayerManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
			}
		}

		private void OnPlayerDataLoaded(PlayerData data)
		{
			_playerData = data;
			RefreshAll();
		}

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
			if (_playerData != null && playerNameText)
				playerNameText.text = _playerData.name;

			_available = Mathf.Max(0, (int)(_playerData?.stats?.pors ?? 0));
			if (!_allocating) _tempRemaining = _available; // cập nhật khi chưa trong mode phân phối
			RefreshAvailablePointsUI();

			var current = _statsManager != null ? GetCurrentStats() : _playerData?.stats;
			foreach (var r in _rows) r.RefreshValue(current);
			UpdateButtonsState();
		}

		private PlayerStats GetCurrentStats()
		{
			// Dùng reflection nhỏ để lấy field private nếu cần, ở đây giả sử có thể expose qua property hoặc serialized ref.
			return PlayerManager.Instance?.Data?.stats;
		}

		private void RefreshAvailablePointsUI()
		{
			if (availablePointsText)
			{
				if (_allocating)
					availablePointsText.text = $"Điểm còn lại: {_tempRemaining}";
				else
					availablePointsText.text = $"Điểm tu luyện: {_available}";
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

			_playerData.stats.pors = _tempRemaining; // cập nhật điểm còn lại
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

		private void ApplyStatIncrease(PlayerStats stats, string id, int add)
		{
			if (stats == null || add <= 0) return;
			switch (id)
			{
				case "hpMax": stats.hpMax += add; break;
				case "qiMax": stats.qiMax += add; break;
				case "atk": stats.atk += add; break;
				case "def": stats.def += add; break;
				case "critRate": stats.critRate += add * 0.5f; break; // ví dụ mỗi điểm +0.5%
				case "critDmg": stats.critDmg += add * 1f; break;
				case "moveSpd": stats.moveSpd += add * 0.2f; break;
				case "hpRegen": stats.hpRegen += add; break;
				case "qiRegen": stats.qiRegen += add; break;
				case "lifesteal": stats.lifesteal += add * 0.5f; break;
				case "spellPower": stats.spellPower += add; break;
				case "spellResist": stats.spellResist += add; break;
				case "dodge": stats.dodge += add * 0.3f; break;
				case "pierce": stats.pierce += add; break;
				default: break;
			}
		}

		private void UpdateButtonsState()
		{
			if (saveButton) saveButton.gameObject.SetActive(_allocating);
			if (cancelButton) cancelButton.gameObject.SetActive(_allocating);
		}
	}
}
