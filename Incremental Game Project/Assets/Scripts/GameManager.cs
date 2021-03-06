using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Range(0f, 1f)]
    public float AutoCollectPrecentage = 0.1f;
    public ResourceConfig[] ResourceConfigs;
    public Sprite[] ResourceSprites;

    public Transform ResourceParent;
    public ResourceController ResourcePrefab;
    public TapText TapTextPrefab;

    public Transform CoinIcon;
    public Text GoldInfo;
    public Text AutoCollectInfo;

    public float SaveDelay = 5f;

    private List<ResourceController> _activeResource = new List<ResourceController>();
    private List<TapText> _tapTextPool = new List<TapText>();
    private float _collectSecond;

    private float _saveDelayCounter;
    

    private static GameManager _instance = null;
    public static GameManager Instance
    {
        get
        {
            if(_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }

            return _instance;
        }
    }

    private void Start()
    {
        AddAllResources();
        GoldInfo.text = $"Gold: {UserDataManager.Progress.Gold.ToString("0")}";
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;
        _saveDelayCounter -= deltaTime;

        _collectSecond += deltaTime;
        if (_collectSecond >= 1f)
        {
            CollectPerSecond();
            _collectSecond = 0f;
        }

        CheckResourceCost();
        CoinIcon.transform.localScale = Vector3.LerpUnclamped(CoinIcon.transform.localScale, Vector3.one * 2f, 0.15f);
        CoinIcon.transform.Rotate(0f, 0f, Time.deltaTime * -100f);
    }

    private void AddAllResources()
    {
        bool showResources = true;
        int index = 0;

        foreach (ResourceConfig config in ResourceConfigs)
        {
            GameObject obj = Instantiate(ResourcePrefab.gameObject, ResourceParent, false);
            ResourceController resource = obj.GetComponent<ResourceController>();

            resource.SetConfig(index, config);

            obj.gameObject.SetActive(showResources);

            if(showResources&& !resource.isUnlocked)
            {
                showResources = false;
            }

            _activeResource.Add(resource);
            index++;
        }
    }

    private void CollectPerSecond()
    {
        double output = 0;
        foreach (ResourceController resource in _activeResource)
        {
            if (resource.isUnlocked)
            {
                output += resource.GetOutput();
            }   
        }

        output *= AutoCollectPrecentage;

        AutoCollectInfo.text = $"Auto Collect: {output.ToString("F1")}/second";

        AddGold(output);
    }

    public void AddGold(double value)
    {
        UserDataManager.Progress.Gold += value;
        GoldInfo.text = $"Gold: {UserDataManager.Progress.Gold.ToString("0")}";
        UserDataManager.Save(_saveDelayCounter<0f);

        if (_saveDelayCounter < 0f)
            _saveDelayCounter = SaveDelay;

        if (UserDataManager.Progress.Gold > 1000000)
        {
            AchievementController.Instance.UnlockAchievement(AchievementType.InfiniteGrowth, "1000000");
        }
    }
    
    public void CollectByTap(Vector3 tapPosition, Transform parent)
    {
        double output = 1;
        foreach(ResourceController resource in _activeResource)
        {
            if (resource.isUnlocked)
            {
                output += resource.GetOutput();
            }
        }

        TapText tapText = GetOrCreateTapText();
        tapText.transform.SetParent(parent, false);
        tapText.transform.position = tapPosition;

        tapText.Text.text = $"+{output.ToString("0")}";
        tapText.gameObject.SetActive(true);
        CoinIcon.transform.localScale = Vector3.one * 1.75f;

        AddGold(output);
    }

    private TapText GetOrCreateTapText()
    {
        TapText tapText = _tapTextPool.Find(t => !t.gameObject.activeSelf);
        if (tapText == null)
        {
            tapText = Instantiate(TapTextPrefab).GetComponent<TapText>();
            _tapTextPool.Add(tapText);
        }

        return tapText;
    }

    public void ShowNextResource()
    {
        foreach (ResourceController resource in _activeResource)
        {
            if (!resource.gameObject.activeSelf)
            {
                resource.gameObject.SetActive(true);
                break;
            }
        }
    }

    private void CheckResourceCost()
    {
        foreach (ResourceController resource in _activeResource)
        {
            bool isBuyable = false;

            if (resource.isUnlocked)
            {
                isBuyable = UserDataManager.Progress.Gold >= resource.GetUpgradeCost();
            }
            else
            {
                isBuyable = UserDataManager.Progress.Gold >= resource.GetUnlockCost();
            }
            resource.ResourceImage.sprite = ResourceSprites[isBuyable ? 1 : 0];
        }
    }
}

[System.Serializable]
public struct ResourceConfig
{
    public string Name;
    public double UnlockCost;
    public double UpgradeCost;
    public double Output;
}
