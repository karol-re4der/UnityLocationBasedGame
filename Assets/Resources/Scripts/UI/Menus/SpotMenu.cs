using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SpotMenu : SubMenu
{
    private SpotData _currentSpot;
    public TextMeshProUGUI NameField;
    public TextMeshProUGUI DescriptionField;
    public TextMeshProUGUI OwnerField;
    public TextMeshProUGUI ValueField;
    public TextMeshProUGUI ButtonText;


    void Update()
    {

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

    }
}
