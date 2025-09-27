using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

namespace VHAssets
{
[Serializable]
public class CutsceneEventParam
{
    //[HideInInspector]
    public int intData;
    public bool boolData;
    public float floatData;
    public string stringData = string.Empty;
    [XmlIgnore()]
    public UnityEngine.Object objData;
    public Vector2 vec2Data;
    public Vector3 vec3Data;
    public Vector4 vec4Data;
    public Color colorData;
    [XmlIgnore()]
    public Enum enumData;

    // used for saving/loading from xml
    // in order to find the correct data references
    [HideInInspector]
    public string objDataName = "";
    [HideInInspector]
    public int objDataInstanceId;
    [HideInInspector]
    public string objDataAssetPath = "";
    [HideInInspector]
    public bool objDataIsComponent;
    [HideInInspector]
    public bool usesObjectData;
    //[HideInInspector]
    public bool usesEnumData;
    //[HideInInspector]
    public string enumDataString = "";

    //[HideInInspector]
    public string DataType;

    [HideInInspector]
    public string Name;

    public CutsceneEventParam() { }
    public CutsceneEventParam(string dataType, string paramName)
    {
        DataType = dataType;
        Name = paramName;
    }

    public CutsceneEventParam Clone()
    {
        CutsceneEventParam cloned = new CutsceneEventParam(DataType, Name);
        cloned.intData = intData;
        cloned.boolData = boolData;
        cloned.floatData = floatData;
        cloned.stringData = stringData;
        cloned.SetObjData(objData);
        cloned.vec2Data = vec2Data;
        cloned.vec3Data = vec3Data;
        cloned.vec4Data = vec4Data;
        cloned.colorData = colorData;
        cloned.enumData = enumData;
        cloned.enumDataString = enumDataString;
        cloned.usesEnumData = usesEnumData;
        return cloned;
    }

    public void SetObjData(UnityEngine.Object data)
    {
        objData = data;
        objDataName = objData != null ? objData.name : "";
        objDataIsComponent = data is Component;
        usesObjectData = objData != null;

        if (objData != null)
        {
            objDataInstanceId = objDataIsComponent ? ((Component)objData).gameObject.GetInstanceID() : objData.GetInstanceID();
        }
        else
        {
            //Debug.LogError("Fail!");
            objDataInstanceId = 0;
        }
    }

    public void SetEnumData(Enum en)
    {
        usesEnumData = true;
        enumData = en;
        enumDataString = en.ToString();
    }

    public void SetLength(float length)
    {
        if (DataType == typeof(int).ToString())
        {
            intData = (int)length;
        }
        else if (DataType == typeof(float).ToString())
        {
            floatData = length;
        }
        else if (objData is Cutscene cutscene)
        {
            cutscene.Length = length;
        }
    }

    public float GetLength()
    {
        float length = -1;
        if (DataType == typeof(int).ToString())
        {
            length = intData;
        }
        else if (DataType == typeof(float).ToString())
        {
            length = floatData;
        }
        else if (objData is AudioClip audioclip)
        {
            length = audioclip.length;
        }
        else if (objData is AnimationClip animclip)
        {
            length = animclip.length;
        }
        else if (objData is Cutscene cutscene)
        {
            length = cutscene.Length;
        }
        else
        {
            //Debug.LogError(string.Format("Couldn't determine the length of parameter {0}", Name));
        }
        return length;
    }

    public static void CopyParameters(CutsceneEventParam src, CutsceneEventParam dest)
    {
        dest.boolData = src.boolData;
        dest.floatData = src.floatData;
        dest.stringData = src.stringData;
        dest.vec2Data = src.vec2Data;
        dest.vec3Data = src.vec3Data;
        dest.vec4Data = src.vec4Data;
        dest.colorData = src.colorData;
        dest.boolData = src.boolData;
        dest.SetObjData(src.objData);
        dest.DataType = src.DataType;
        dest.enumData = src.enumData;
        dest.enumDataString = src.enumDataString;
        dest.usesEnumData = src.usesEnumData;
        dest.Name = src.Name;
    }
}

[System.Serializable]
public class CutsceneEvent : CutsceneTrackItem
{
    #region Constants
    public const string NoneParameter = "None";
    #endregion

    #region Variables
    [XmlIgnore]
    public GameObject TargetGameObject;

    [XmlIgnore]
    public Component TargetComponent;

    // used for xml saving
    [HideInInspector]
    public string TargetGameObjectName = "";

    [HideInInspector]
    public int TargetGameObjectInstanceId;

    [HideInInspector]
    public string TargetComponentName = "";

    public string FunctionName;

    [HideInInspector]
    public int FunctionOverloadIndex;

    [HideInInspector]
    public string LengthDefiningParamName = "";

    public List<CutsceneEventParam> m_Params = new List<CutsceneEventParam>();

    ICutsceneEventInterface m_EventInterface;
    object m_MetaData;
    #endregion

    #region Functions

    public CutsceneEvent() { }

    public CutsceneEvent(Rect guiPosition, string uniqueId)
        : base(guiPosition, uniqueId) { }

    public void CloneData(CutsceneEvent receiver)
    {
        receiver.Name = Name;
        receiver.EventType = EventType;
        receiver.SetFunctionTargets(TargetGameObject, TargetComponent);
        receiver.FunctionName = FunctionName;
        receiver.FunctionOverloadIndex = FunctionOverloadIndex;
        receiver.LengthRepresented = LengthRepresented;
        receiver.FireAndForget = FireAndForget;
        receiver.LengthDefiningParamName = LengthDefiningParamName;
        //receiver.SetLocked(Locked);
        receiver.SetEnabled(Enabled);

        if (receiver.LengthRepresented)
        {
            receiver.Length = Length;
        }

        if (TargetComponent != null)
        {
            receiver.m_EventInterface = ((GenericEvents)TargetComponent).CreateCutsceneEventInterfaceFromMethod(FunctionName);
        }

        CloneParams(receiver);
    }

    public void CloneParams(CutsceneEvent receiver)
    {
        receiver.m_Params = new List<CutsceneEventParam>(m_Params.Count);
        for (int i = 0; i < m_Params.Count; i++)
        {
            receiver.m_Params.Add(m_Params[i].Clone());
        }
    }

    public void Init()
    {
        CreateEventInterface();
    }

    void CreateEventInterface()
    {
        if (m_EventInterface == null && !GenericEventNames.IsCustomEvent(EventType))
        {
            if (TargetComponent != null)
            {
                m_EventInterface = ((GenericEvents)TargetComponent).CreateCutsceneEventInterfaceFromMethod(FunctionName);
            }
            else
            {
                Debug.LogError(string.Format("Cutscene Event {0} {1} does not have a TargetComponent. Delete and re-create the event", Name, FunctionName));
            }
        }
    }

    /// <summary>
    /// Invokes the functionality that the event should perform. Returns if the event is disabled
    /// </summary>
    /// <param name="time"></param>
    public void Fire(float time)
    {
        if (!Enabled)
        {
            return;
        }

        object[] funcParams = new object[m_Params.Count];
        int i = 0;
        foreach (CutsceneEventParam param in m_Params)
        {
            if (param.DataType == typeof(int).ToString())
            {
                funcParams[i] = param.intData;
            }
            else if (param.DataType == typeof(bool).ToString())
            {
                funcParams[i] = param.boolData;
            }
            else if (param.DataType == typeof(float).ToString())
            {
               funcParams[i] = param.floatData;
            }
            else if (param.DataType == typeof(string).ToString())
            {
                funcParams[i] = param.stringData;
            }
            else if (param.DataType == typeof(Vector2).ToString())
            {
                funcParams[i] = param.vec2Data;
            }
            else if (param.DataType == typeof(Vector3).ToString())
            {
                funcParams[i] = param.vec3Data;
            }
            else if (param.DataType == typeof(Vector4).ToString())
            {
                funcParams[i] = param.vec4Data;
            }
            else if (param.DataType == typeof(Color).ToString())
            {
                funcParams[i] = param.colorData;
            }
            else if (param.usesEnumData)
            {
                funcParams[i] = Enum.Parse(Type.GetType(param.DataType), param.enumDataString);
            }
            else // we default it to this
            {
                funcParams[i] = param.objData;
                if (funcParams[i] == null)
                {
                    Debug.LogError(string.Format("Parameter {0} on event {1} was left blank", param.Name, Name));
                }
            }
            ++i;
        }

        try
        {
            if (GenericEventNames.IsCustomEvent(EventType))
            {
                MethodInfo method = Array.FindAll<MethodInfo>(TargetComponent.GetType().GetMethods(), delegate(MethodInfo meth) { return meth.Name == FunctionName; })[FunctionOverloadIndex];
                method.Invoke(TargetComponent, funcParams);
            }
            else
            {
                ((GenericEvents)TargetComponent).InvokeEventMethod(FunctionName, FunctionOverloadIndex, funcParams, time, m_MetaData, (GenericEvents)TargetComponent);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(string.Format("Error occured when trying to fire event {0}. Exception {1}. Inner Exeception: {2}. Stack: {3}", Name, e.Message, e.InnerException, e.StackTrace));
        }
    }

    public bool HasDataToRecord()
    {
        return m_EventInterface != null;
    }

    public object SaveRewindData()
    {
        return m_EventInterface?.SaveRewindData(this);
    }

    public void Pause()
    {
        m_EventInterface?.Pause(this);
    }

    public void LoadRewindData(object rewindData)
    {
        m_EventInterface?.LoadRewindData(this, rewindData);
    }

    public void UseParamDefaultValue(CutsceneEventParam param)
    {
        if (!GenericEventNames.IsCustomEvent(EventType))
        {
            CreateEventInterface();
            if (m_EventInterface != null)
            {
                ((GenericEvents)TargetComponent).UseParamDefaultValue(this, param);
            }
        }
    }

    public void SetFunctionTargets(GameObject targetGO, Component targetComp)
    {
        TargetGameObject = targetGO;
        TargetComponent = targetComp;

        TargetGameObjectName = TargetGameObject != null ? TargetGameObject.name : "";
        TargetGameObjectInstanceId = TargetGameObject != null ? TargetGameObject.GetInstanceID() : 0;
        TargetComponentName = TargetComponent != null ? TargetComponent.GetType().ToString() : "";
    }

    public void SetEventType(string eventType)
    {
        EventType = eventType;

        if (GenericEventNames.IsCustomEvent(eventType))
        {
            FireAndForget = true;
            LengthRepresented = false;
        }
        else
        {
            FireAndForget = ((GenericEvents)TargetComponent).IsEventMethodFireAndForget(FunctionName);
            LengthRepresented = !FireAndForget;
        }
    }

    public bool NeedsToBeFired()
    {
        bool needsToBeFired = true; // custom events will always get fired since we can't specify
        if (!GenericEventNames.IsCustomEvent(EventType))
        {
            needsToBeFired = ((GenericEvents)TargetComponent).NeedsToBeFired(FunctionName, this);
        }
        return needsToBeFired;
    }

    public string GetXMLString()
    {
        string retVal = string.Empty;
        CreateEventInterface();
        if (m_EventInterface != null)
        {
            retVal = ((GenericEvents)TargetComponent).GetXMLString(this);
        }
        return retVal;
    }

    public void SetParameters(XmlReader reader)
    {
        CreateEventInterface();
        if (m_EventInterface != null)
        {
            ((GenericEvents)TargetComponent).SetParameters(this, reader);
        }
    }

    public void SetMetaData(object metaData)
    {
        m_MetaData = metaData;
    }

    public string GetLengthParameterName()
    {
        string retVal = NoneParameter;
        CreateEventInterface();
        if (m_EventInterface != null)
        {
            return ((GenericEvents)TargetComponent).GetLengthDefiningParamFromMethod(FunctionName);
        }

        return retVal;
    }

    public CutsceneEventParam GetLengthParameter()
    {
        return FindParameter(GetLengthParameterName());
    }

    public void SetEventLengthFromParameter(string paramName)
    {
        LengthDefiningParamName = paramName;
        CreateEventInterface();

        if (!GenericEventNames.IsCustomEvent(EventType) && !FireAndForget)
        {
            LengthRepresented = true;
            return;
        }

        float length = -1;
        if (!GenericEventNames.IsCustomEvent(EventType))
        {
            // they have their own way of calculating the length
            length = ((GenericEvents)TargetComponent).CalculateEventLength(FunctionName, this);
        }
        else if (paramName != NoneParameter)
        {
            CutsceneEventParam cep = m_Params.Find(p => p.Name == paramName);
            if (cep == null)
            {
                Debug.LogError(string.Format("Parameter named {0} doesn't exist for function {1} using overload index {2}", paramName, FunctionName, FunctionOverloadIndex));
            }
            else
            {
                length = cep.GetLength();
            }
        }

        if (length > 0)
        {
            // found the parameter and it's legit
            Length = length;
            LengthRepresented = true;
        }
        else
        {
            // param wasn't found or wasn't a representable number
            Length = 1;
            LengthRepresented = false;
        }
    }

    /// <summary>
    /// Returns the CutsceneEventParam data of the given parameter name
    /// </summary>
    /// <param name="parameterName"></param>
    /// <returns></returns>
    public CutsceneEventParam FindParameter(string parameterName)
    {
        CutsceneEventParam retVal = m_Params.Find(cep => cep.Name == parameterName);
        if (retVal == null && parameterName != NoneParameter)
        {
            Debug.LogError(string.Format("Couldn't find parameter with name {0} in cutscene event {1}", parameterName, Name));
        }

        return retVal;
    }

    public CutsceneEventParam FindParameter(int paramIndex)
    {
        CutsceneEventParam retVal = null;
        if (paramIndex < 0 || paramIndex >= m_Params.Count)
        {
            Debug.LogError(string.Format("Parameter index {0} out of range int cutscene event {1}", paramIndex, Name));
        }
        else
        {
            retVal = m_Params[paramIndex];
        }
        return retVal;
    }

    public bool DoesParameterExist(string parameterName)
    {
        return FindParameterIndex(parameterName) != -1;
    }

    /// <summary>
    /// Returns the 0 based index ordering of the given parameter for the function that uses it
    /// </summary>
    /// <param name="parameterName"></param>
    /// <returns></returns>
    public int FindParameterIndex(string parameterName)
    {
        return m_Params.FindIndex(cep => cep.Name == parameterName);
    }

    /// <summary>
    /// Sets the new function name that this event will call
    /// </summary>
    /// <param name="functionName"></param>
    public void ChangedEventFunction(string functionName)
    {
        ChangedEventFunction(functionName, 0);
    }

    /// <summary>
    /// Sets the new function name and overload index that this event will call
    /// </summary>
    /// <param name="functionName"></param>
    /// <param name="overloadIndex"></param>
    public void ChangedEventFunction(string functionName, int overloadIndex)
    {
        FunctionName = functionName;
        FunctionOverloadIndex = overloadIndex;
        if (GenericEventNames.IsCustomEvent(EventType))
        {
            FireAndForget = true;
        }
        else
        {
            FireAndForget = ((GenericEvents)TargetComponent).IsEventMethodFireAndForget(FunctionName);
        }
        GatherEventParams(TargetComponent, FunctionName, FunctionOverloadIndex);
    }

    /// <summary>
    /// Returns all the available overloads of the provided function
    /// </summary>
    /// <param name="behaviour"></param>
    /// <param name="functionName"></param>
    /// <param name="ce"></param>
    /// <returns></returns>
    public MethodInfo[] GetFunctionOverloads(Component behaviour, string functionName)
    {
        MethodInfo[] functionOverloads = null;
        if (GenericEventNames.IsCustomEvent(EventType))
        {
            functionOverloads = Array.FindAll<MethodInfo>(behaviour.GetType().GetMethods(), m => m.Name == functionName);
        }
        else
        {
            functionOverloads = ((GenericEvents)behaviour).GetEventMethodOverloads(functionName);
        }

        if (functionOverloads == null)
        {
            Debug.LogError(string.Format("{0} doesn't have a function named {1}", behaviour.name, functionName));
        }

        return functionOverloads;
    }

    /// <summary>
    /// Returns the parameter list of the given function overload
    /// </summary>
    /// <param name="behaviour"></param>
    /// <param name="functionName"></param>
    /// <param name="overloadIndex"></param>
    /// <param name="eventType"></param>
    /// <returns></returns>
    public ParameterInfo[] GetFunctionParams(Component behaviour, string functionName, int overloadIndex, string eventType)
    {
        if (behaviour == null && GenericEventNames.IsCustomEvent(eventType)) // <--- TODO: check this. This test is failing even when the behaviour is not null. This started happening in unity 4.3
        {
            // still null, get out of here
            return null;
        }

        if (GenericEventNames.IsCustomEvent(eventType))
        {
            MethodInfo[] functionOverloads = Array.FindAll<MethodInfo>(behaviour.GetType().GetMethods(), m => m.Name == functionName);

            if (functionOverloads == null || functionOverloads.Length == 0)
            {
                //Debug.LogError(string.Format("{0} doesn't have a function named {1}", behaviour.name, functionName));
                return null;
            }
            if (overloadIndex >= functionOverloads.Length)
            {
                Debug.LogError(string.Format("{0} doesn't have an overload index of {1}. Length is {2}", behaviour.name, overloadIndex, functionOverloads.Length));
                return null;
            }

            return functionOverloads[overloadIndex].GetParameters();
        }
        else
        {
            ParameterInfo[] functionParams = ((GenericEvents)behaviour).GetEventMethodParams(functionName, overloadIndex);
            if (functionParams == null)
            {
                overloadIndex = FunctionOverloadIndex = 0;
                functionParams = ((GenericEvents)behaviour).GetEventMethodParams(functionName, overloadIndex);
            }
            return functionParams;
        }
    }

    public ParameterInfo[] GetFunctionParams()
    {
        //Debug.Log("TargetComponent.name = " + TargetComponent.name);
        return GetFunctionParams(TargetComponent, FunctionName, FunctionOverloadIndex, EventType);
    }

    /// <summary>
    /// Loads the parameters that are required for the specific function overload
    /// </summary>
    /// <param name="behaviour"></param>
    /// <param name="functionName"></param>
    /// <param name="overloadIndex"></param>
    /// <param name="ce"></param>
    public void GatherEventParams(Component behaviour, string functionName, int overloadIndex)
    {
        ParameterInfo[] funcParams = GetFunctionParams(behaviour, functionName, overloadIndex, EventType);
        List<CutsceneEventParam> prevParams = new List<CutsceneEventParam>(m_Params);
        m_Params.Clear();

        if (funcParams == null || funcParams.Length == 0)
        {
            // this functions takes no arguements
            return;
        }

        for (int i = 0; i < funcParams.Length; i++)
        {
            m_Params.Add(new CutsceneEventParam(funcParams[i].ParameterType.ToString(), funcParams[i].Name));
        }

        // try to preserve previous arguments being passed
        for (int i = 0; i < m_Params.Count; i++)
        {
            CutsceneEventParam matchingParam = prevParams.Find(prev => prev.Name == m_Params[i].Name && prev.DataType == m_Params[i].DataType);
            if (matchingParam != null)
            {
                m_Params[i] = matchingParam.Clone();
            }
            else
            {
                UseParamDefaultValue(m_Params[i]);
            }
        }
    }
    #endregion
}
}
