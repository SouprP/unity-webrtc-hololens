using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IJsonObject<T>
{
    string ConvertToJSON();

    static T FromJSON(string jsonString) => throw new NotImplementedException();
}
