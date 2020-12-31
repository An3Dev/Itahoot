using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class Card : MonoBehaviour
{

    public TextMeshProUGUI nameText, timeText, answerText, pointsText, lobbyText;
    public GameObject selectionPanel;
    public Button button;

    AnswerInfo answerInfo;
    long points;
    bool selected = false;
    bool interactable = false;
    string name;
    public Card()
    {
        points = 0;
    }

    public void ShowAnswer()
    {
        answerText.text = answerInfo.answer;
        answerText.fontStyle = FontStyles.Normal;
    }

    public void SetAnswerInfo(AnswerInfo answerInfo)
    {
        this.answerInfo = answerInfo;
        timeText.text = answerInfo.time.ToString();
        answerText.text = "Answer is hidden.";
        answerText.fontStyle = FontStyles.Italic;
    }
    public AnswerInfo GetAnswerInfo()
    {
        return answerInfo;
    }

    public void SetInteractable(bool isInteractable)
    {
        interactable = isInteractable;
        button.interactable = interactable;
    }

    public void InLobby(string name)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        lobbyText.text = name;
        lobbyText.gameObject.SetActive(true);
    }

    public void SetName(string name)
    {
        this.name = name;
        nameText.text = name;
    }

    public void SetPoints(long points)
    {
        this.points = points;
        pointsText.text = "+" + points;
    }
    public float GetTime()
    {
        return answerInfo.time;
    }

    public bool IsSelected()
    {
        return selected;
    }

    public void OnClick()
    {
        selectionPanel.SetActive(!selected);
        selected = !selected;
    }
}
