using Court;
using System;
using TMPro;
using Unity.Netcode;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.UI;

public class CourtUI : UIHUD
{
    private TMP_Text voteNumberText;
    private TMP_Text voteRankingText;
    private Slider timeSlider;

    private VoteModel voteModel;
    private CourtController courtController;

    enum Texts
    {
        Votes_Number_Text,
        Votes_Ranking_Text,
    }

    enum Sliders
    {
        Time_Slider
    }

    private void Start()
    {
        courtController = FindFirstObjectByType<CourtController>().GetComponent<CourtController>();

        base.Init();
        Bind<TextMeshProUGUI>(typeof(Texts));
        Bind<Slider>(typeof(Sliders));

        voteNumberText = Get<TextMeshProUGUI>((int)Texts.Votes_Number_Text);
        voteRankingText = Get<TextMeshProUGUI>((int)Texts.Votes_Ranking_Text);

        timeSlider = Get<Slider>((int)Sliders.Time_Slider);

        voteNumberText.text = VoteModel.Instance.GetPlayerVoteCount((int)NetworkManager.Singleton.LocalClientId).ToString();
        voteRankingText.text = VoteModel.Instance.GetPlayerRank((int)NetworkManager.Singleton.LocalClientId).ToString();

    }

    private void FixedUpdate()
    {
        timeSlider.value = courtController.GetTimeRatio();
    }
}
