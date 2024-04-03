using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class BulletUI : MonoBehaviour
{
    private PlayerControl playerControl;
    private TextMeshProUGUI rightBulletText;
    private TextMeshProUGUI leftBulletText;
    public GameObject pistolPanel;
    private GameObject leftContainer;
    private GameObject rightContainer;
    [SerializeField] private GameObject bulletPrefab;
    private GameObject notEnoughBulletPanel;
    private TextMeshProUGUI notEnoughBulletText;
    private Coroutine FadeNotEnoughBulletPanelCoroutine;
    private float fadeTime = 3f;

    private Image leftBulletRemainingTimeImage;
    private Image rightBulletRemainingTimeImage;

    public void Awake()
    {
        playerControl = GetComponent<PlayerControl>();
        rightBulletText = pistolPanel.transform.Find("right_pistol_bullet_text_panel/right_pistol_text").GetComponent<TextMeshProUGUI>();
        leftBulletText = pistolPanel.transform.Find("left_pistol_bullet_text_panel/left_pistol_text").GetComponent<TextMeshProUGUI>();
        leftContainer = pistolPanel.transform.Find("left_pistol_icon/BulletContainer").gameObject;
        rightContainer = pistolPanel.transform.Find("right_pistol_icon/BulletContainer").gameObject;
        // bulletPrefab = Resources.Load<GameObject>("Prefabs/bullet_icon");
        notEnoughBulletPanel = pistolPanel.transform.Find("not_enough_bullet_panel").gameObject;
        notEnoughBulletText = notEnoughBulletPanel.transform.Find("not_enough_bullet_text").GetComponent<TextMeshProUGUI>();
        notEnoughBulletPanel.SetActive(false);
        leftBulletRemainingTimeImage = pistolPanel.transform.Find("left_pistol_icon/ReloadBar").GetComponent<Image>();
        leftBulletRemainingTimeImage.fillAmount = 0f;
        rightBulletRemainingTimeImage = pistolPanel.transform.Find("right_pistol_icon/ReloadBar").GetComponent<Image>();
        rightBulletRemainingTimeImage.fillAmount = 0f;
    }

    public void UpdateUI()
    {
        rightBulletText.text = playerControl.rightPistolBulletCount.ToString();
        leftBulletText.text = playerControl.leftPistolBulletCount.ToString();
        for (int i = 0; i < leftContainer.transform.childCount; i++)
        {
            leftContainer.transform.GetChild(i).gameObject.SetActive(i < playerControl.rightPistolBulletCount);
        }
        for (int i = 0; i < rightContainer.transform.childCount; i++)
        {
            rightContainer.transform.GetChild(i).gameObject.SetActive(i < playerControl.leftPistolBulletCount);
        }
    }

    public void RiseNotEnoughBulletPanel()
    {

        if (FadeNotEnoughBulletPanelCoroutine != null)
        {
            StopCoroutine(FadeNotEnoughBulletPanelCoroutine);
        }
        notEnoughBulletPanel.SetActive(true);
        Color textColor = notEnoughBulletText.color;
        textColor.a = 1f;
        notEnoughBulletText.color = textColor;
        Color panelColor = notEnoughBulletPanel.GetComponent<Image>().color;
        panelColor.a = 1f;
        notEnoughBulletPanel.GetComponent<Image>().color = panelColor;
        FadeNotEnoughBulletPanelCoroutine = StartCoroutine(FadeNotEnoughBulletPanel());
    }
    public IEnumerator FadeNotEnoughBulletPanel()
    {
        float elapsedTime = 0f;
        Image notEnoughBulletPanelImage = notEnoughBulletPanel.GetComponent<Image>();
        Color textColor = notEnoughBulletText.color;
        Color panelColor = notEnoughBulletPanelImage.color;

        while (elapsedTime < fadeTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
            textColor.a = alpha;
            panelColor.a = alpha;

            notEnoughBulletText.color = textColor;
            notEnoughBulletPanelImage.color = panelColor;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        notEnoughBulletPanel.SetActive(false);
    }

    public void UpdateLeftBulletRemainingTime(float percentage)
    {
        leftBulletRemainingTimeImage.fillAmount = percentage;
    }

    public void UpdateRightBulletRemainingTime(float percentage)
    {
        rightBulletRemainingTimeImage.fillAmount = percentage;
    }
}
