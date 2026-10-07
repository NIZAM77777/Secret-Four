using UnityEngine;
namespace SecretFour.Presentation
{
    // Matches the world-space board to the width-scaled UI on taller portrait screens.
    // Logical cells, placements, moves and input handlers are untouched.
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Secret Four/UI/Portrait Board Camera")]
    public class PortraitBoardCamera : MonoBehaviour
    {
        public float referenceOrthographicSize=6f;
        public float referenceAspect=9f/16f;
        private Camera targetCamera;
        void Awake(){targetCamera=GetComponent<Camera>();Fit();}
        void LateUpdate()=>Fit();
        void Fit(){if(targetCamera==null||!targetCamera.orthographic)return;targetCamera.orthographicSize=referenceOrthographicSize*referenceAspect/Mathf.Max(.1f,targetCamera.aspect);}
    }
}
