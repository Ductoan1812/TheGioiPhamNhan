using UnityEngine;
using TMPro;

public class FloatingCombatText : MonoBehaviour
{
	[Header("Config")] [SerializeField] private float lifetime = 1.1f;
	[SerializeField] private Vector2 moveUpRange = new Vector2(0.8f, 1.1f);
	[SerializeField] private float randomX = 0.3f;
	[SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
	[SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0.9f, 1, 1f);
	[SerializeField] private float critScale = 1.4f;

	private TMP_Text label;
	private float startTime;
	private Vector3 startWorldPos;
	private float travel;
	private bool active;
	private float scaleMul = 1f;
	private bool screenSpace;

	private void Awake()
	{
		label = GetComponent<TMP_Text>();
	}

	public void Show(string text, Color color, Vector3 worldPos, bool crit = false)
	{
		if (!label) return;
		scaleMul = crit ? critScale : 1f;
		label.text = text;
		label.color = color;
		startTime = Time.time;
		startWorldPos = worldPos + new Vector3(Random.Range(-randomX, randomX), 0f, 0f);
		travel = Random.Range(moveUpRange.x, moveUpRange.y);
		active = true;
		screenSpace = FloatingCombatTextSpawner.ScreenSpace;
		RefreshPosition(0f);
		gameObject.SetActive(true);
	}

	private void Update()
	{
		if (!active) return;
		float t = (Time.time - startTime) / lifetime;
		if (t >= 1f)
		{
			Recycle();
			return;
		}
		float a = alphaCurve.Evaluate(t);
		float s = scaleCurve.Evaluate(t) * scaleMul;
		if (label)
		{
			var c = label.color; c.a = a; label.color = c;
			label.rectTransform.localScale = Vector3.one * s;
		}
		RefreshPosition(t);
	}

	private void RefreshPosition(float t)
	{
		Vector3 worldPos = startWorldPos + Vector3.up * (travel * t);
		if (screenSpace)
		{
			var cam = FloatingCombatTextSpawner.UICamera;
			Vector3 sp = cam ? cam.WorldToScreenPoint(worldPos) : Camera.main.WorldToScreenPoint(worldPos);
			transform.position = sp;
		}
		else
		{
			transform.position = worldPos;
		}
	}

	private void Recycle()
	{
		active = false;
		if (FloatingCombatTextSpawner.InstanceFCT) FloatingCombatTextSpawner.InstanceFCT.Recycle(this);
		else gameObject.SetActive(false);
	}
}

