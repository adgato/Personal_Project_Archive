using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Music_Theory;

[System.Serializable]
public class ProgressionConstructer : IConstructer<Progression>
{
    [ReadOnly] [SerializeField] Progression scale;
    [Tooltip("Format: The sequence is prefixed with 's'. The numbers are Duodecimal (base-12), from 1 to C, and their sum is twelve.")]
    [SerializeField] string scaleLookup;
    [Min(0)]
    [SerializeField] int option;

    Progression _scale;

    public Progression Get() => scale;


    public void Set(Progression scale)
    {
        this.scale = scale;
        Validate();
    }

    public void Validate()
    {
        if (scale != _scale)
        {
            _scale = scale;
            scaleLookup = scale.ToString();
            return;
        }
        if (scaleLookup == null)
            scaleLookup = "";

        int maxScore = 0;
        List<Progression> matches = new List<Progression>();
        matches.Add(scale);
        for (Progression i = Progression.s111111111111; i <= Progression.sC; i++)
        {
            int score = 0;
            List<char> chars = i.ToString().ToCharArray().ToList();
            foreach (char c in scaleLookup)
                if (chars.Contains(c))
                {
                    score++;
                    chars.Remove(c);
                }
            if (score > maxScore)
            {
                maxScore = score;
                matches.Clear();
                matches.Add(i);
            }
            else if (score == maxScore)
                matches.Add(i);
        }
        option %= matches.Count;
        scale = _scale = matches[option];
    }
}
