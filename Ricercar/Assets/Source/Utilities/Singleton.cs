using UnityEngine;

[ExecuteInEditMode]
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    
    public static T Instance
    {
        get
        {
            if (!instance)
                instance = (T)FindObjectOfType(typeof(T));

            return instance;
        }
    }

    protected virtual void OnEnable()
    {
        CheckInstance();
    }

    protected bool CheckInstance()
    {
        if (!instance)
        {
            instance = (T)this;
            return true;
        }
        else if (instance == this)
        {
            return true;
        }

        Debug.LogWarning("Destroying duplicate " + typeof(T) + " singleton!", gameObject);
        Destroy(this);
        return false;
    }
}
