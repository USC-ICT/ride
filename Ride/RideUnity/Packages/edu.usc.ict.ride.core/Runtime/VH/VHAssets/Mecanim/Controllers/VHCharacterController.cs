using UnityEngine;

namespace VHAssets
{
// INSTRUCTIONS, DERIVED CLASSES
// - Derived classes need to provide description and aniamtion guidlines.
// - Derived classes may override start, but if so they need to also run the base Start function since it has certain checks in it.
public class VHCharacterController : MonoBehaviour, ICharacterFunctionality
{
    #region Variables
    /// <summary>
    /// This value represents how important this controller is compared to others. This is used when GetComponents is called to sort out
    /// the most important controller of a specific type on a gameobject
    /// The higher the number, the more important
    /// </summary>
    [SerializeField]
    int m_priority; 

    #endregion

    // ---------------------------------------------------------------------------------------------
    #region Properties
    
    public string AnimationGuidelines { get; set; }    // instructions on animations creaed for this controller
    
    public string ControllerDescription{ get; set; }    // description of the controllers purpose
    
    public int Priority
    {
        get { return m_priority; }
        set { m_priority = value; }
    }
    
    public string Id { get { return GetType().ToString(); } }
    
    #endregion

    // ---------------------------------------------------------------------------------------------
    #region Functions
    public virtual void Start()
    {
    }

    public virtual void Update()
    {
    }
    #endregion
}
}
