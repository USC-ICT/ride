using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VHAssets
{
// INSTRUCTIONS, DERIVED CLASSES
// - Derived classes need to provide layers and parameters used.

/// <summary>
/// A controller created with the purpose of providing concrete functionality for manipulating a character.  
/// Example: the BlinkController's purpose is to make the character blink via Unity's animator controller, blend shapes,
/// or any preferred method. A functionality controller can be unit tested simply by placing it on a gameobject. Generally speaking,
/// functionality controllers have few (if any) dependencies on other classes.  No manager controllers are required to be present in 
/// order for a functionality controller to work.
/// </summary>
[ExecuteInEditMode]
public abstract class FunctionalityController : VHCharacterController
{
    // Animator controller used by controllers
    protected Animator m_animator;

    // Layer(s) and parameter(s) required by derived classes
    protected List<AnimContParameter> m_parameters = new List<AnimContParameter>();
    
    // Weight of affect layer
    public float m_layerWeight;

    
    // ---------------------------------------------------------------------------------------------
    #region Properties

    public Animator Animator
    {
        get { return m_animator; }
    }


    public List<string> Layers { get; set; }

    public List<AnimContParameter> Parameters
    {
        get { return m_parameters; }
    }

    public float LayerWeight
    {
        get
        {
            return m_layerWeight;
        }
        set
        {
            m_layerWeight = value;
            if (m_animator != null)
            {
                if (Application.isPlaying)
                {
                    if (VHMecanimUtils.LayerExists(Layers[0], m_animator))
                    {
                        VHMecanimUtils.LayerSetWeight(Layers[0], m_animator, m_layerWeight);
                    }
                }
            }   
        }
    }

    #endregion

    // ---------------------------------------------------------------------------------------------
    /// <summary>
    /// Class to store information about an AnimatorController parameter.
    /// </summary>
    public class AnimContParameter
    {
        string m_name;
        AnimatorControllerParameterType m_type;


        public string name
        {
            get { return m_name;  }
        }

        public AnimatorControllerParameterType type
        {
            get { return m_type; }
        }


        public AnimContParameter(string name, AnimatorControllerParameterType type)
        {
            m_name = name;
            m_type = type;
        }
    }


    // ---------------------------------------------------------------------------------------------
    #region Unity Event Functions
    
//    void Awake(){
//        m_animator = gameObject.GetComponent<Animator>();
//        if (m_animator == null){
//            Debug.LogError("Did not find Animator component!", this);
//        }
//    }


//    public override void Start()
//    {
//        base.Start(); 
//        checkAnimatorController();
//    }

    
    public override void Update()
    {
        base.Update();
        m_animator = gameObject.GetComponent<Animator>();
    }

    #endregion

    // Functions -----------------------------------------------------------------------------------
    /// <summary>
    /// Add a required Animator Controller parameter to the functionality controller.
    /// </summary>
    /// <param name="paramName"></param>
    /// <param name="paramType"></param>
    /// <returns></returns>
    protected AnimContParameter AddRequiredParameter(string paramName, AnimatorControllerParameterType paramType)
    {
        AnimContParameter newParam = new AnimContParameter(paramName, paramType);
        m_parameters.Add(newParam);
        return newParam;
    }

    /// <summary>
    /// Check that the animator controller meets the requirments of this component.
    /// </summary>
    private void checkAnimatorController()
    {
        // Check that correct layer exists
        foreach (string i in Layers)
        {
            if (VHMecanimUtils.LayerExists(i, m_animator) != true)
            {
                Debug.LogError("Did not find required animator controller layer: <color=red>"+i+"</color>", this);
            }
        }

        // Check that correct parameter exist
        foreach (AnimContParameter i in m_parameters)
        {
            if (VHMecanimUtils.ParameterExists(i.name, m_animator) != true)
            {
                Debug.LogError("Did not find required animator controller parameter: <color=red>"+i+"</color>", this);
            }
        }
    }
}
}
