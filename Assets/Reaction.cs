using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Reaction : MonoBehaviour
{
    public GameObject[] players;
    private TagGameManager tagScript;

    void Awake()
    {
        tagScript = TagGameManager.Instance;
    }

    void Update()
    {
        if (tagScript.matchOver)
        {
            PlayerReaction(tagScript.Winner);
        }
    }

    private void PlayerReaction(GameObject winner)
    {
        foreach (GameObject player in players)
        {
            Animator anim = player.GetComponent<Animator>();

            if (player == winner)
            {
                anim.SetBool("Win", true);
            }
            else
            {
                anim.SetBool("Lose", true);
            }
        }
    }
}
