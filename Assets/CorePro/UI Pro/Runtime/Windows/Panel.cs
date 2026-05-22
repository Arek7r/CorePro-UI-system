using InspectorPro;
using UnityEngine;
using TMPro;

namespace CorePro.UI
{
    /// <summary>
    /// Example panel demonstrating how to extend PanelBase.
    /// 
    /// Usage from code:
    ///   settingsPanel.Show();
    ///   settingsPanel.Hide();
    ///
    /// Usage via UIPanelManager:
    ///   manager.OpenPanel("Settings");
    ///   manager.Back();
    /// </summary>
    public class Panel : PanelBase
    {
        
    }
}

// =============================================================
// HOW TO USE - QUICK REFERENCE
// =============================================================
//
// 1. Create your panel class:
//    public class MainMenuPanel : PanelBase { ... }
//
// 2. In Inspector (UIPanelManager):
//    - Add entries to Panels list
//    - Drag PanelBase subcomponent into each slot
//    - Use slider to set Default Panel
//    - Per panel: override anim duration, disable culling, toggle default
//
// 3. Open/close from code:
//    manager.OpenPanel("Settings");     // by name
//    manager.OpenPanel(2);              // by index
//    manager.Back();                    // go back in history
//    manager.Next();                    // go to next
//
// 4. Open/close from anywhere (standalone panel):
//    settingsPanel.Show();
//    settingsPanel.Hide();
//    settingsPanel.Toggle();
//    settingsPanel.ShowImmediate();
//
// 5. Hooks (override in subclass):
//    OnBeforeShow()  - init data before animation
//    OnAfterShow()   - panel is fully visible
//    OnBeforeHide()  - save state before animation  
//    OnAfterHide()   - panel is fully gone
//
// =============================================================