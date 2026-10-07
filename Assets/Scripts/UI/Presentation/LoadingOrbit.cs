using UnityEngine;
using UnityEngine.UI;
namespace SecretFour.Presentation
{
    public class LoadingOrbit : MonoBehaviour
    {
        public float degreesPerSecond=75;
        void Update(){transform.Rotate(0,0,-degreesPerSecond*Time.unscaledDeltaTime);}
    }
}
