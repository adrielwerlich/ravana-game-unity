using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputActionsSingleton
{
    private static RavanaInputActions instance;

    public static RavanaInputActions Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new RavanaInputActions();
            }
            return instance;
        }
    }
}
