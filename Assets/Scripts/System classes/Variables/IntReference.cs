using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IntReference
{
    public bool UseConstant = true;
    public int ConstValue;
    public IntVariable Variable;
    public int Value
    {
        get { return UseConstant ? ConstValue : Variable.Value; }
    }
}
