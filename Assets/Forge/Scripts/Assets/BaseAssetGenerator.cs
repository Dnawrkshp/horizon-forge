using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseAssetGenerator : MonoBehaviour
{
    public abstract void Generate();

    public virtual void OnPreBake() { }
    public virtual void OnPostBake() { }
}
