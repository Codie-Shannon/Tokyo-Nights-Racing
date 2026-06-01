using TMPro;
using UnityEngine;

public class RaceHUDUI : MonoBehaviour
{
    [Header("References")]
    public RaceManager raceManager;
    public RacePositionManager racePositionManager;

    [Header("HUD Text")]
    public TMP_Text raceNameText;
    public TMP_Text lapText;
    public TMP_Text positionText;
    public TMP_Text timeText;

    [Header("Options")]
    public bool hideWhenNoRace = true;

    private void Update()
    {
        if (raceManager == null)
            return;

        bool raceAvailable = raceManager.GetCurrentRace() != null;

        if (hideWhenNoRace && !raceAvailable)
        {
            ClearTexts();
            return;
        }

        UpdateRaceName();
        UpdateLap();
        UpdatePosition();
        UpdateTime();
    }

    private void UpdateRaceName()
    {
        if (raceNameText == null)
            return;

        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace != null)
            raceNameText.text = currentRace.raceDisplayName;
        else
            raceNameText.text = "";
    }

    private void UpdateLap()
    {
        if (lapText == null)
            return;

        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace == null)
        {
            lapText.text = "";
            return;
        }

        if (currentRace.raceType == RaceType.Circuit)
        {
            lapText.text = raceManager.GetCurrentLap() + "/" + currentRace.laps;
        }
        else
        {
            lapText.text = "SPRINT";
        }
    }

    private void UpdatePosition()
    {
        if (positionText == null)
            return;

        if (racePositionManager == null)
        {
            positionText.text = "--";
            return;
        }

        int position = racePositionManager.GetPlayerPosition();
        positionText.text = FormatPosition(position);
    }

    private void UpdateTime()
    {
        if (timeText == null)
            return;

        float time = raceManager.GetCurrentRaceTime();
        timeText.text = FormatTime(time);
    }

    private void ClearTexts()
    {
        if (raceNameText != null)
            raceNameText.text = "";

        if (lapText != null)
            lapText.text = "";

        if (positionText != null)
            positionText.text = "--";

        if (timeText != null)
            timeText.text = "00:00.00";
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;

        return string.Format("{0:00}:{1:00.00}", minutes, seconds);
    }

    private string FormatPosition(int position)
    {
        if (position <= 0)
            return "--";

        if (position % 100 >= 11 && position % 100 <= 13)
            return position + "th";

        switch (position % 10)
        {
            case 1:
                return position + "st";

            case 2:
                return position + "nd";

            case 3:
                return position + "rd";

            default:
                return position + "th";
        }
    }
}