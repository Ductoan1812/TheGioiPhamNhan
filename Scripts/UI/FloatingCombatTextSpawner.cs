using System.Collections.Generic;
using UnityEngine;

public class FloatingCombatTextSpawner : MonoBehaviour
{
	public static FloatingCombatTextSpawner InstanceFCT { get; private set; }

	[SerializeField] private FloatingCombatText prefab;
	[SerializeField] private int prewarm = 10;
	[SerializeField] private Transform parentOverride;
	[Header("Canvas Mode")]
	[SerializeField, Tooltip("Bật nếu Canvas dùng Screen Space (Overlay / Camera). Nếu World Space thì tắt.")] private bool screenSpaceCanvas = false;
	[SerializeField, Tooltip("Camera của Canvas Screen Space - Camera (nếu để null sẽ dùng Camera.main)")] private Camera uiCamera;
	[Header("Colors")] [SerializeField] private Color damageColor = Color.red;
	[SerializeField] private Color healColor = Color.green;
	[SerializeField] private Color expColor = new Color(1f, 0.85f, 0.2f);
	[SerializeField, Tooltip("Màu hiển thị item / vàng")] private Color itemColor = new Color(0.9f, 0.9f, 0.9f);

	private readonly Queue<FloatingCombatText> pool = new();

	private void Awake()
	{
		if (InstanceFCT && InstanceFCT != this)
		{
			Destroy(gameObject);
			return;
		}
		InstanceFCT = this;
		if (!prefab)
		{
			Debug.LogWarning("FloatingCombatTextSpawner: prefab null");
			return;
		}
		for (int i = 0; i < prewarm; i++) CreateNew();
	}

	private FloatingCombatText CreateNew()
	{
		var f = Instantiate(prefab, parentOverride ? parentOverride : transform);
		f.gameObject.SetActive(false);
		pool.Enqueue(f);
		return f;
	}

	private FloatingCombatText Get()
	{
		if (pool.Count == 0) CreateNew();
		return pool.Dequeue();
	}

	public void Recycle(FloatingCombatText f)
	{
		f.gameObject.SetActive(false);
		pool.Enqueue(f);
	}

	public void ShowDamage(Vector3 pos, int amount, bool crit = false)
	{
		var f = Get();
		f.Show("-" +amount.ToString(), damageColor, pos, crit);
	}
	public void ShowHeal(Vector3 pos, int amount)
	{
		var f = Get();
		f.Show("+" + amount, healColor, pos, false);
	}
	public void ShowExp(Vector3 pos, int amount)
	{
		var f = Get();
		f.Show("+" + amount + " EXP", expColor, pos, false);
	}
	public void ShowItem(Vector3 pos, int amount, string itemName)
	{
		var f = Get();
		f.Show("+" + amount + " " + itemName, itemColor, pos, false);
	}

	#region Static Accessors
	public static bool ScreenSpace => InstanceFCT && InstanceFCT.screenSpaceCanvas;
	public static Camera UICamera => InstanceFCT && InstanceFCT.uiCamera ? InstanceFCT.uiCamera : Camera.main;
	#endregion
}

