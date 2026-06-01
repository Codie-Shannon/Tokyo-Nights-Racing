using UnityEngine;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("References")]
    public CarController playerCar;
    public RaceManager raceManager;
    public RacePositionManager racePositionManager;

    [Header("UI")]
    public TMP_Text speedText;
    public TMP_Text lapText;
    public TMP_Text positionText;
    public TMP_Text raceStateText;
    public TMP_Text raceNameText;
    public TMP_Text currentLapTimeText;
    public TMP_Text bestLapTimeText;
    public TMP_Text currentRaceTimeText;

    void Update()
    {
        UpdateSpeed();
        UpdateRaceName();
        UpdateLap();
        UpdateCurrentLapTime();
        UpdateBestLapTime();
        UpdateCurrentRaceTime();
        UpdatePosition();
        UpdateRaceState();
    }

    void UpdateSpeed()
    {
        if (playerCar == null || speedText == null) return;

        Rigidbody rb = playerCar.GetComponent<Rigidbody>();
        if (rb == null) return;

        float speed = rb.velocity.magnitude * 3.6f;
        speedText.text = Mathf.RoundToInt(speed).ToString("000") + " KPH";
    }

    void UpdateRaceName()
    {
        if (raceNameText == null) return;

        if (raceManager == null)
        {
            raceNameText.text = "";
            return;
        }

        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace == null)
        {
            raceNameText.text = "";
            return;
        }

        raceNameText.text = currentRace.raceDisplayName;
    }

    void UpdateLap()
    {
        if (raceManager == null || lapText == null) return;

        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace == null)
        {
            lapText.text = "";
            return;
        }

        switch (currentRace.raceType)
        {
            case RaceType.Circuit:
                lapText.text = "Lap " + raceManager.GetCurrentLap() + "/" + currentRace.laps;
                break;

            case RaceType.PointToPoint:
                lapText.text = "Sprint";
                break;

            case RaceType.Offroad:
                lapText.text = "Offroad";
                break;

            default:
                lapText.text = "";
                break;
        }
    }

    void UpdateCurrentLapTime()
    {
        if (currentLapTimeText == null) return;

        if (raceManager == null)
        {
            currentLapTimeText.text = "";
            return;
        }

        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace == null || currentRace.raceType != RaceType.Circuit)
        {
            currentLapTimeText.text = "";
            return;
        }

        currentLapTimeText.text = "Lap Time: " + FormatTime(raceManager.GetCurrentLapTime());
    }

    void UpdateBestLapTime()
    {
        if (bestLapTimeText == null) return;

        if (raceManager == null)
        {
            bestLapTimeText.text = "";
            return;
        }

        RaceDefinition currentRace = raceManager.GetCurrentRace();

        if (currentRace == null || currentRace.raceType != RaceType.Circuit)
        {
            bestLapTimeText.text = "";
            return;
        }

        float bestLap = raceManager.GetBestLapTime();

        if (bestLap <= 0f)
        {
            bestLapTimeText.text = "Best Lap: --";
            return;
        }

        bestLapTimeText.text = "Best Lap: " + FormatTime(bestLap);
    }

    void UpdateCurrentRaceTime()
    {
        if (currentRaceTimeText == null) return;

        if (raceManager == null || raceManager.GetCurrentRace() == null)
        {
            currentRaceTimeText.text = "";
            return;
        }

        float timeToShow = raceManager.IsRaceFinished()
            ? raceManager.GetFinalRaceTime()
            : raceManager.GetCurrentRaceTime();

        currentRaceTimeText.text = "Time: " + FormatTime(timeToShow);
    }

    void UpdatePosition()
    {
        if (positionText == null) return;

        if (racePositionManager == null)
        {
            positionText.text = "";
            return;
        }

        int pos = racePositionManager.GetPlayerPosition();
        positionText.text = FormatPosition(pos);
    }

    void UpdateRaceState()
    {
        if (raceStateText == null) return;

        if (raceManager == null)
        {
            raceStateText.text = "";
            return;
        }

        if (raceManager.IsRaceFinished())
        {
            raceStateText.text = "Finished!";
            return;
        }

        if (raceManager.IsCountdownActive())
        {
            raceStateText.text = Mathf.CeilToInt(raceManager.GetCountdownTime()).ToString();
            return;
        }

        if (raceManager.IsRaceActive())
        {
            raceStateText.text = "Racing";
        }
        else
        {
            raceStateText.text = "";
        }
    }

    string FormatPosition(int pos)
    {
        if (pos <= 0) return "--";

        if (pos % 100 >= 11 && pos % 100 <= 13)
            return pos + "th";

        switch (pos % 10)
        {
            case 1: return pos + "st";
            case 2: return pos + "nd";
            case 3: return pos + "rd";
            default: return pos + "th";
        }
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return string.Format("{0:00}:{1:00.00}", minutes, seconds);
    }
}