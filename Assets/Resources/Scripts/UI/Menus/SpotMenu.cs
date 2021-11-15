using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SpotMenu : SubMenu
{
    private SpotData _currentSpot;
    public TextMeshProUGUI NameField;
    public TextMeshProUGUI DescriptionField;
    public TextMeshProUGUI OwnerField;
    public TextMeshProUGUI ValueField;
    public TextMeshProUGUI ButtonText;
    public Button PurchaseButton;

    void Update()
    {
        if (Globals.GetClientLogic().LatestPlayerData!=null && _currentSpot!=null) {
            if (Globals.GetClientLogic().LatestPlayerData.Value >= _currentSpot.Value)
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
        _currentSpot = spot;
        FillFields();
        base.Enter();
    }

    private void FillFields()
    {
        NameField.text = _currentSpot.Name;
        DescriptionField.text = _currentSpot.Description;
        OwnerField.text = _currentSpot.OwnerNickname;
        ValueField.text = $"{_currentSpot.IncomePerSecond} {Globals.ValueChar}/s";
        ButtonText.text = _currentSpot.Value + " " + Globals.ValueChar;
    }

    public void Button_Purchase()
    {
        string message = ClientAPI.Prepare_BUY(PlayerPrefs.GetString("Token", ""), _currentSpot.Id);
        Globals.GetNetworkManager().SendMessageToServer("BUY", message);
    }
}
