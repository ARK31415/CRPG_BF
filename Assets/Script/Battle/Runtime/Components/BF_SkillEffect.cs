using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 法师技能使用的最小独立效果表现。
/// </summary>
public class BF_SkillEffect : MonoBehaviour
{
    public void Play(
        BF_BoardManager board,
        Vector2Int actorPos,
        IReadOnlyList<Vector2Int> path,
        float duration)
    {
        if (path.Count == 0)
        {
            transform.position = board.GridToWorld(actorPos);
            Destroy(gameObject, duration);
            return;
        }

        transform.position = board.GridToWorld(actorPos);
        StartCoroutine(Move(board, path, duration));
    }

    private IEnumerator Move(
        BF_BoardManager board,
        IReadOnlyList<Vector2Int> path,
        float duration)
    {
        float stepTime = duration / path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 from = transform.position;
            Vector3 to = board.GridToWorld(path[i]);
            float time = 0f;

            while (time < stepTime)
            {
                time += Time.deltaTime;
                transform.position = Vector3.Lerp(from, to, time / stepTime);
                yield return null;
            }
        }

        Destroy(gameObject);
    }
}
