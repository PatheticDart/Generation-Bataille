using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LoadEnemyData : MonoBehaviour
{
    public EnemyDataSO enemyData;

    public TextMeshProUGUI enemyNameText, enemyHealthText, enemyDamageText, enemyDescriptionText;
    public TextMeshProUGUI buttonLabel;

    public GameObject spawnPointObject;
    public Transform spawnPoint;

    public Sprite DRankSprite, CRankSprite, BRankSprite, ARankSprite, SRankSprite, SSRankSprite, SSSRankSprite;
    public Image rankSpriteRenderer;
    public GameObject rankSpriteObject;
    public GameObject arenaEnterBtn;

    public bool mapSelected = false;
    public GarageGameTransition transitionManager;
    

    void Start()
    {
        if (buttonLabel != null && enemyData != null)
        {
            buttonLabel.text = enemyData.enemyName;
        }
        if (rankSpriteObject != null)
        {
            rankSpriteObject.SetActive(false);
        }
    }

    void Update()
    {
        mapSelected = transitionManager.mapSelected;
    }

    public void LoadData()
    {
        transitionManager.enemySelected = true;
        if (arenaEnterBtn != null && mapSelected == true)
        {
            arenaEnterBtn.SetActive(true);
        }
        else if (arenaEnterBtn != null && mapSelected == false)
        {
            arenaEnterBtn.SetActive(false);
        }
        if (rankSpriteObject != null){
            rankSpriteObject.SetActive(true);
        }
        if (enemyData == null)
        {
            Debug.LogError($"LoadEnemyData: enemyData is not assigned on '{gameObject.name}'", this);
            return;
        }

        if (enemyNameText != null) enemyNameText.text = enemyData.enemyName;
        if (enemyHealthText != null) enemyHealthText.text = enemyData.health.ToString();
        if (enemyDamageText != null) enemyDamageText.text = enemyData.damage.ToString();
        if (enemyDescriptionText != null) enemyDescriptionText.text = enemyData.description;

        // Choose parent transform
        Transform parent = null;
        if (spawnPointObject != null)
            parent = spawnPointObject.transform;
        else if (spawnPoint != null)
            parent = spawnPoint;

        if (parent == null)
        {
            Debug.LogError($"LoadEnemyData: No spawn point assigned (spawnPointObject or spawnPoint) on '{gameObject.name}'", this);
        }
        else
        {
            if (parent.childCount > 0)
            {
                foreach (Transform child in parent)
                {
                    Destroy(child.gameObject);
                }
            }

            if (enemyData.enemyPrefab != null)
            {
                Instantiate(enemyData.enemyPrefab, parent.position, Quaternion.identity, parent);
            }
            else
            {
                Debug.LogError($"LoadEnemyData: enemyPrefab is null on EnemyDataSO '{enemyData.name}'", this);
            }
        }

        if (rankSpriteRenderer == null)
        {
            Debug.LogError($"LoadEnemyData: rankSpriteRenderer is not assigned on '{gameObject.name}'", this);
            return;
        }
        LoadRank(enemyData.rank);
    }
    
    public void LoadRank(EnemyDataSO.EnemyRank rank)
    {
        if (rankSpriteRenderer == null)
        {
            Debug.LogError($"LoadEnemyData: rankSpriteRenderer is not assigned on '{gameObject.name}'", this);
            return;
        }

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
        }

        rankSpriteRenderer.SetNativeSize();
    }
}
