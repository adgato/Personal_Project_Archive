using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Music_Theory;

public abstract class InstrumentConstructer : ScriptableObject, IConstructer<Instrument>
{
    public abstract Instrument Get();
}
