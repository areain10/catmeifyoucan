using UnityEngine;
using UnityEngine.U2D.Animation;

public class CustomizationManager : MonoBehaviour
{
    public enum PlayerID { Player1, Player2 }
    public enum PartType { Body, Ears, Muzzle, Paws }

    [System.Serializable]
    public class CharacterParts
    {
        public SpriteLibrary body;
        public SpriteLibrary ears;
        public SpriteLibrary muzzle;
        public SpriteLibrary paws;
    }

    [Header("Menu Preview Characters")]
    public CharacterParts player1Preview;
    public CharacterParts player2Preview;

    [Header("Actual In-Game Characters")]
    public CharacterParts player1Real;
    public CharacterParts player2Real;

    [Header("Available Options")]
    public SpriteLibraryAsset[] bodyLibraries;
    public SpriteLibraryAsset[] earLibraries;
    public SpriteLibraryAsset[] muzzleLibraries;
    public SpriteLibraryAsset[] pawsLibraries;

    private int[] p1Value = new int[4];
    private int[] p2Value = new int[4];

    public void NextBodyP1() => NextOption(PlayerID.Player1, PartType.Body);
    public void NextEarsP1() => NextOption(PlayerID.Player1, PartType.Ears);
    public void NextMuzzleP1() => NextOption(PlayerID.Player1, PartType.Muzzle);
    public void NextPawsP1() => NextOption(PlayerID.Player1, PartType.Paws);

    public void NextBodyP2() => NextOption(PlayerID.Player2, PartType.Body);
    public void NextEarsP2() => NextOption(PlayerID.Player2, PartType.Ears);
    public void NextMuzzleP2() => NextOption(PlayerID.Player2, PartType.Muzzle);
    public void NextPawsP2() => NextOption(PlayerID.Player2, PartType.Paws);

    public void NextOption(PlayerID player, PartType part)
    {
        int partIndex = (int)part;

        int[] value = (player == PlayerID.Player1) ? p1Value : p2Value;
        SpriteLibraryAsset[] libraryArray = GetLibraryArray(part);

        value[partIndex] = (value[partIndex] + 1) % libraryArray.Length;

        ApplyToPreview(player, part, libraryArray[value[partIndex]]);
    }

    void ApplyToPreview(PlayerID player, PartType part, SpriteLibraryAsset asset)
    {
        CharacterParts target = (player == PlayerID.Player1) ? player1Preview : player2Preview;

        SpriteLibrary partLibrary = GetPartLibrary(target, part);

        if (asset == null)
        {
            partLibrary.gameObject.SetActive(false);
        }
        else
        {
            partLibrary.gameObject.SetActive(true);
            partLibrary.spriteLibraryAsset = asset;
        }
    }

    public void ApplyToRealCharacters()
    {
        ApplyFullCharacter(player1Real, p1Value);
        ApplyFullCharacter(player2Real, p2Value);
    }

    void ApplyFullCharacter(CharacterParts target, int[] value)
    {
        ApplyPart(target.body, bodyLibraries[value[0]]);
        ApplyPart(target.ears, earLibraries[value[1]]);
        ApplyPart(target.muzzle, muzzleLibraries[value[2]]);
        ApplyPart(target.paws, pawsLibraries[value[3]]);
    }

    void ApplyPart(SpriteLibrary lib, SpriteLibraryAsset asset)
    {
        if (asset == null)
        {
            lib.gameObject.SetActive(false);
        }
        else
        {
            lib.gameObject.SetActive(true);
            lib.spriteLibraryAsset = asset;
        }
    }

    SpriteLibraryAsset[] GetLibraryArray(PartType part)
    {
        switch (part)
        {
            case PartType.Body: return bodyLibraries;
            case PartType.Ears: return earLibraries;
            case PartType.Muzzle: return muzzleLibraries;
            case PartType.Paws: return pawsLibraries;
        }
        return null;
    }

    SpriteLibrary GetPartLibrary(CharacterParts character, PartType part)
    {
        switch (part)
        {
            case PartType.Body: return character.body;
            case PartType.Ears: return character.ears;
            case PartType.Muzzle: return character.muzzle;
            case PartType.Paws: return character.paws;
        }
        return null;
    }
}