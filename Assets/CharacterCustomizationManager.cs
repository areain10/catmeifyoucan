using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D.Animation;

[Serializable]
public class CharacterPreview
{
    public string playerName;

    public SpriteResolver body, ears, muzzle, paws;

    [HideInInspector] public int bodyIndex = 0;
    [HideInInspector] public int earsIndex = 0;
    [HideInInspector] public int muzzleIndex = 0;
    [HideInInspector] public int pawsIndex = 0;
}

public static class PlayerCustomizationData
{
    public static string P1Body, P1Ears, P1Muzzle, P1Paws;
    public static string P2Body, P2Ears, P2Muzzle, P2Paws;
}

public class CharacterCustomizationManager : MonoBehaviour
{
    [Header("Players")]
    public CharacterPreview player1, player2;

    [Header("Options (Categories in your SpriteLibrary)")]
    public string[] bodyOptions;
    public string[] earsOptions;
    public string[] muzzleOptions;
    public string[] pawsOptions;

    [Header("Buttons (order: Body, Ears, Muzzle, Paws)")]
    public Button[] player1Buttons;
    public Button[] player2Buttons;

    private const string label = "forward0"; // default frame to show

    private void Start()
    {
        InitPlayer(player1, bodyOptions, earsOptions, muzzleOptions, pawsOptions);
        InitPlayer(player2, bodyOptions, earsOptions, muzzleOptions, pawsOptions);

        // Hook up buttons
        for (int i = 0; i < 4; i++)
        {
            int layerIndex = i;
            if (player1Buttons.Length > i) player1Buttons[i].onClick.AddListener(() => CycleLayer(player1, layerIndex));
            if (player2Buttons.Length > i) player2Buttons[i].onClick.AddListener(() => CycleLayer(player2, layerIndex));
        }
    }

    private void InitPlayer(CharacterPreview player, string[] bodyOpts, string[] earsOpts, string[] muzzleOpts, string[] pawsOpts)
    {
        player.bodyIndex = 0;
        player.earsIndex = 0;
        player.muzzleIndex = 0;
        player.pawsIndex = 0;
        ApplyPlayer(player);
    }

    private void CycleLayer(CharacterPreview player, int layerIndex)
    {
        switch (layerIndex)
        {
            case 0: player.bodyIndex = (player.bodyIndex + 1) % bodyOptions.Length; break;
            case 1: player.earsIndex = (player.earsIndex + 1) % earsOptions.Length; break;
            case 2: player.muzzleIndex = (player.muzzleIndex + 1) % muzzleOptions.Length; break;
            case 3: player.pawsIndex = (player.pawsIndex + 1) % pawsOptions.Length; break;
        }
        ApplyPlayer(player);
    }

    private void ApplyPlayer(CharacterPreview player)
    {
        if (player.body != null) player.body.SetCategoryAndLabel(bodyOptions[player.bodyIndex], label);
        if (player.ears != null) player.ears.SetCategoryAndLabel(earsOptions[player.earsIndex], label);
        if (player.muzzle != null) player.muzzle.SetCategoryAndLabel(muzzleOptions[player.muzzleIndex], label);
        if (player.paws != null) player.paws.SetCategoryAndLabel(pawsOptions[player.pawsIndex], label);
    }

    public void SaveSelections()
    {
        PlayerCustomizationData.P1Body = bodyOptions[player1.bodyIndex];
        PlayerCustomizationData.P1Ears = earsOptions[player1.earsIndex];
        PlayerCustomizationData.P1Muzzle = muzzleOptions[player1.muzzleIndex];
        PlayerCustomizationData.P1Paws = pawsOptions[player1.pawsIndex];

        PlayerCustomizationData.P2Body = bodyOptions[player2.bodyIndex];
        PlayerCustomizationData.P2Ears = earsOptions[player2.earsIndex];
        PlayerCustomizationData.P2Muzzle = muzzleOptions[player2.muzzleIndex];
        PlayerCustomizationData.P2Paws = pawsOptions[player2.pawsIndex];

        Debug.Log("Player selections saved!");
    }
}