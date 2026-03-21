using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadRankData : MonoBehaviour
{
    public Sprite DRankSprite, CRankSprite, BRankSprite, ARankSprite, SRankSprite, SSRankSprite, SSSRankSprite;
    public Image rankSpriteRenderer;

    public LoadEnemyData loadEnemyDataScript;

    // void Update()
    // {
    //     if (loadEnemyDataScript.enemyData != null)
    //     {
    //         return;
    //     }
    //     else{
    //         switch (loadEnemyDataScript.enemyData.rank)
    //         {
    //             case EnemyDataSO.EnemyRank.D:
    //                 rankSpriteRenderer.sprite = DRankSprite;
    //                 break;
    //             case EnemyDataSO.EnemyRank.C:
    //                 rankSpriteRenderer.sprite = CRankSprite;
    //                 break;
    //             case EnemyDataSO.EnemyRank.B:
    //                 rankSpriteRenderer.sprite = BRankSprite;
    //                 break;
    //             case EnemyDataSO.EnemyRank.A:
    //                 rankSpriteRenderer.sprite = ARankSprite;
    //                 break;
    //             case EnemyDataSO.EnemyRank.S:
    //                 rankSpriteRenderer.sprite = SRankSprite;
    //                 break;
    //             case EnemyDataSO.EnemyRank.SS:
    //                 rankSpriteRenderer.sprite = SSRankSprite;
    //                 break;
    //             case EnemyDataSO.EnemyRank.SSS:
    //                 rankSpriteRenderer.sprite = SSSRankSprite;
    //                 break;
    //         }
    //     }
    // }

    public void switchRankImage()
    {
        if (loadEnemyDataScript == null)
        {
            Debug.LogError("LoadRankData: loadEnemyDataScript is not assigned.", this);
            return;
        }

        if (loadEnemyDataScript.enemyData == null)
        {
            Debug.LogError("LoadRankData: enemyData is null on the referenced LoadEnemyData.", this);
            return;
        }

        if (rankSpriteRenderer == null)
        {
            Debug.LogError("LoadRankData: rankSpriteRenderer Image is not assigned.", this);
            return;
        }

        EnemyDataSO.EnemyRank rank = loadEnemyDataScript.enemyData.rank;
        Debug.Log($"LoadRankData: switching rank image to '{rank}' for '{loadEnemyDataScript.enemyData.enemyName}'", this);

        switch (rank)
        {
            case EnemyDataSO.EnemyRank.D:
                rankSpriteRenderer.sprite = DRankSprite;
                break;
            case EnemyDataSO.EnemyRank.C:
                rankSpriteRenderer.sprite = CRankSprite;
                break;
            case EnemyDataSO.EnemyRank.B:
                rankSpriteRenderer.sprite = BRankSprite;
                break;
            case EnemyDataSO.EnemyRank.A:
                rankSpriteRenderer.sprite = ARankSprite;
                break;
            case EnemyDataSO.EnemyRank.S:
                rankSpriteRenderer.sprite = SRankSprite;
                break;
            case EnemyDataSO.EnemyRank.SS:
                rankSpriteRenderer.sprite = SSRankSprite;
                break;
            case EnemyDataSO.EnemyRank.SSS:
                rankSpriteRenderer.sprite = SSSRankSprite;
                break;
            default:
                Debug.LogWarning($"LoadRankData: unhandled rank '{rank}'", this);
                break;
        }

        rankSpriteRenderer.SetNativeSize();
    }
}
