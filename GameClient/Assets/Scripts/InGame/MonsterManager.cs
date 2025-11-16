using InGame;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    public HashSet<EnemyController> enemyControllers = new HashSet<EnemyController>();

    public int GetActiveMonsterCount()
    {
        return enemyControllers != null ? enemyControllers.Count : 0;
    }
}
