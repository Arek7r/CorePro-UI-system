using UnityEngine;

namespace CorePro.UI
{
    public class SelectorIndicator : MonoBehaviour
    {
        [SerializeField] private Transform stateOn;
        [SerializeField] private Transform stateOff;

        public void SetActive(bool isActive)
        {
            stateOn.gameObject.SetActive(isActive);
            stateOff.gameObject.SetActive(!isActive);
        }
    }
}