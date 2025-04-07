using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankBox : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI rankText;

    [SerializeField]
    private TextMeshProUGUI scoreText;

    [SerializeField]
    private TextMeshProUGUI nickNameText;

    [SerializeField]
    private Image profileImage;

    public void Initialize(RankData rankData)
    {
        rankText.text = rankData.rank.ToString();
        scoreText.text = rankData.score.ToString();
        nickNameText.text = rankData.nickName;
        if (rankData.profileImage != null)
        {
            profileImage.sprite = rankData.profileImage;
        }
    }
}
