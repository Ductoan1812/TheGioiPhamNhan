using System.Collections;
using UnityEngine;
[RequireComponent(typeof(EnemyStats))]
public class EnemyDeathHandler : MonoBehaviour
{
	[Header("Thời gian")] [SerializeField] float deathAnimationDuration = 0.8f; [SerializeField] float respawnDelay = 10f;
	[Header("Tuỳ chọn")] [SerializeField] bool disableAIWhileDead = true; [SerializeField, Tooltip("Respawn ngẫu nhiên bên trong vùng tuần tra của EnemyControler thay vì đúng tâm spawn")] private bool respawnRandomInsidePatrol = true;

	EnemyStats stats; EnemyControler controller; EnemyAttack attack; EnemyAnimation enemyAnim; Animator animator; SpriteRenderer sr; Collider2D[] colliders; bool isDead; Vector2 baseSpawnCenter;

	void Awake(){stats=GetComponent<EnemyStats>();controller=GetComponent<EnemyControler>();attack=GetComponent<EnemyAttack>();enemyAnim=GetComponent<EnemyAnimation>();animator=GetComponent<Animator>();sr=GetComponent<SpriteRenderer>();colliders=GetComponents<Collider2D>();baseSpawnCenter=controller?controller.SpawnPosition:(Vector2)transform.position;stats.onDeath.AddListener(OnDeath);} 

	void OnDeath(){if(isDead)return;isDead=true;if(animator){if(animator.HasParameter("Dead"))animator.SetBool("Dead",true);else animator.SetTrigger("Dead");}if(disableAIWhileDead){if(controller)controller.enabled=false;if(attack)attack.enabled=false;}var rb=GetComponent<Rigidbody2D>();if(rb)rb.linearVelocity=Vector2.zero;StartCoroutine(DeathSequence());}

	System.Collections.IEnumerator DeathSequence(){if(deathAnimationDuration>0f)yield return new WaitForSeconds(deathAnimationDuration);SetVisible(false);if(respawnDelay>0f)yield return new WaitForSeconds(respawnDelay);Respawn();}

	void SetVisible(bool v){if(sr)sr.enabled=v;if(colliders!=null){foreach(var c in colliders)if(c)c.enabled=v;}}

	void Respawn(){
		stats.ResetFullHealth();
		// Dùng tâm patrol từ controller nếu có (ổn định), fallback baseSpawnCenter.
		Vector2 center = controller? controller.SpawnPosition : baseSpawnCenter;
		float radius = controller? controller.PatrolRadius : 0f;
		Vector2 target = center;
		if (respawnRandomInsidePatrol && radius > 0.001f)
		{
			target = center + Random.insideUnitCircle * radius * 0.95f;
		}
		// Đảm bảo chắc chắn không vượt ra ngoài (phòng trường hợp sai số / thay đổi radius runtime)
		if (radius > 0.001f)
		{
			Vector2 offset = target - center;
			float sqrR = radius * radius;
			if (offset.sqrMagnitude > sqrR)
			{
				target = center + offset.normalized * (radius * 0.95f);
			}
		}
		transform.position = target;
		if (animator && animator.HasParameter("Dead")) animator.SetBool("Dead", false);
		SetVisible(true);
		if (controller) controller.enabled = true;
		if (attack) attack.enabled = true;
		isDead = false;
	}
}

static class AnimatorExt{public static bool HasParameter(this Animator a,string n){if(!a)return false;foreach(var p in a.parameters)if(p.name==n)return true;return false;}}
