using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SpotMenu : SubMenu
{
    public SpotData CurrentSpot;
    public TextMeshProUGUI NameField;
    public TextMeshProUGUI DescriptionField;
    public TextMeshProUGUI OwnerField;
    public TextMeshProUGUI ValueField;
    public TextMeshProUGUI ButtonText;
    public Button PurchaseButton;

    void Update()
    {
        if (Globals.GetClientLogic().LatestPlayerData!=null && CurrentSpot!=null) {
            if (Globals.GetClientLogic().LatestPlayerData.Value >= CurrentSpot.Value && Globals.GetClientLogic()?.LatestUserData.Nickname.Equals(CurrentSpot.OwnerNickname)==false)
            {
                PurchaseButton.interactable = true;
            }
            else
            {
                PurchaseButton.interactable = false;
            }
        }
    }

    public void Enter(SpotData spot)
    {
        CurrentSpot = spot;
        FillFields();
        base.Enter();
    }

    private void FillFields()
    {
        NameField.text = CurrentSpot.Name;
        DescriptionField.text = CurrentSpot.Description;
        OwnerField.text = CurrentSpot.OwnerNickname;
        ValueField.text = $"{CurrentSpot.IncomePerSecond} {Globals.ValueChar}/s";
        ButtonText.text = CurrentSpot.Value + " " + Globals.ValueChar;
    }

    public void Button_Purchase()
    {
        string message = ClientAPI.Prepare_BUY(PlayerPrefs.GetString("Token", ""), CurrentSpot.Id);
        Globals.GetNetworkManager().SendMessageToServer("BUY", message);
    }
}
