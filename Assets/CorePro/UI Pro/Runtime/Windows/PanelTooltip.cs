using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CorePro.UI
{
    public class PanelTooltip : MonoBehaviour
    {
        [SerializeField] private TMP_Text primaryText;
        [SerializeField] private TMP_Text secondaryText;

        private StringBuilder _stringBuilder = new StringBuilder();

        public void SetPositionAtPointer()   
        {   
            transform.position = Pointer.current.position.ReadValue();
        }
        
        public void SetPositionAt(Vector2 position)   
        {   
            transform.position = position;
        }
    
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Sets both texts. Secondary is hidden if null or empty
        /// </summary>
        public void SetText(string newPrimaryText, string newSecondaryText = null)
        {
            // Primary text
            if (this.primaryText != null)
            {
                _stringBuilder.Clear();
                if (!string.IsNullOrEmpty(newPrimaryText))
                {
                    _stringBuilder.Append(newPrimaryText);
                }
                this.primaryText.SetText(_stringBuilder);
                this.primaryText.gameObject.SetActive(!string.IsNullOrEmpty(newPrimaryText));
            }

            // Secondary text
            if (this.secondaryText != null)
            {
                bool hasSecondaryText = !string.IsNullOrEmpty(newSecondaryText);
                
                if (hasSecondaryText)
                {
                    _stringBuilder.Clear();
                    _stringBuilder.Append(newSecondaryText);
                    this.secondaryText.SetText(_stringBuilder);
                }
                
                this.secondaryText.gameObject.SetActive(hasSecondaryText);
            }
        }
    }
}