using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;

namespace VHAssets
{
//[ExecuteInEditMode]
public class GenericEvents : MonoBehaviour
{
    #region Variables
    protected List<MethodInfo> m_EventFunctions = new List<MethodInfo>();
    #endregion

    #region Properties
    public MethodInfo[] EventMethods
    {
        get { return m_EventFunctions.ToArray(); }
    }
    #endregion

    #region Functions
    public virtual string GetEventType() { return GetType().ToString(); }

    void Start()
    {
        enabled = !Application.isPlaying;
    }

    //public virtual void Update()
    //{
    //    /*if (!Application.isPlaying)
    //    {
    //        CheckAvailableEvents();
    //    }*/
    //}

    protected static T GetComponentFromString<T>(string gameObjectName) where T : Component
    {
        T comp = default;
        GameObject go = GameObject.Find(gameObjectName);
        if (go != null)
        {
            comp = go.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogError("Gameobject " + gameObjectName + " doesn't have a renderer component");
            }
        }
        else
        {
            Debug.LogError("Can't find gameobject " + gameObjectName);
        }
        return comp;
    }

    public GenericEvents GetGenericEventsByEventType(string eventType)
    {
        GenericEvents[] genericEvents = GetComponents<GenericEvents>();
        GenericEvents match = Array.Find<GenericEvents>(genericEvents, ge => ge.GetEventType() == eventType);

        if (match == null)
        {
            Debug.LogError(string.Format("Couldn't find GenericEvents with type {0}", eventType));
        }

        return match;
    }

    /// <summary>
    /// Refreshes the event list in the Machinima Maker
    /// </summary>
    public void CheckAvailableEvents()
    {
#if !UNITY_WSA
        m_EventFunctions.Clear();
        Type[] nestedTypes = GetType().GetNestedTypes();

        for (int i = 0; i < nestedTypes.Length; i++)
        {
            if (nestedTypes[i].IsClass)
            {
                MethodInfo[] methods = nestedTypes[i].GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance); // only implemented functions
                for (int j = 0; j < methods.Length; j++)
                {
                    // ignore methods that are inherited from ICutsceneEventInterface
                    MethodInfo[] ignoredMethods = typeof(ICutsceneEventInterface).GetMethods();
                    if (Array.Exists<MethodInfo>(ignoredMethods, delegate(MethodInfo info) { return info.Name == methods[j].Name; }))
                    {
                        continue;
                    }
                    m_EventFunctions.Add(methods[j]);
                }
            }
        }
#else
        Debug.LogErrorFormat("GenericEvents.CheckAvailableEvents() - not implemented on this platform.");
#endif
    }

    public string GetLengthDefiningParamFromMethod(string eventMethodName)
    {
        return GetReturnValueFromFunction<string>(eventMethodName, "GetLengthParameterName", null);
    }

    public bool IsEventMethodFireAndForget(string eventMethodName)
    {
        return GetReturnValueFromFunction<bool>(eventMethodName, "IsFireAndForget", null);
    }

    /// <summary>
    /// This is used when we are fast forwarding a cutscene and we're trying to maintain how
    /// the cutscene would look if we played it through from the start
    /// </summary>
    /// <returns><c>true</c>, if to be fired was needsed, <c>false</c> otherwise.</returns>
    /// <param name="eventMethodName">Event method name.</param>
    public bool NeedsToBeFired(string eventMethodName, CutsceneEvent ce)
    {
        object[] parameters = new object[1] { ce };
        return GetReturnValueFromFunction<bool>(eventMethodName, "NeedsToBeFired", parameters);
    }

    //public void Fast

    /// <summary>
    /// Returns the length in time of the event
    /// </summary>
    /// <param name="eventMethodName"></param>
    /// <param name="ce"></param>
    /// <returns></returns>
    public float CalculateEventLength(string eventMethodName, CutsceneEvent ce)
    {
        object[] parameters = new object[1] { ce };
        return GetReturnValueFromFunction<float>(eventMethodName, "CalculateEventLength", parameters);
    }

    public string GetXMLString(CutsceneEvent ce)
    {
        object[] parameters = new object[1] { ce };
        return GetReturnValueFromFunction<string>(ce.FunctionName, "GetXMLString", parameters);
    }

    /// <summary>
    /// Uses attribute values from the xml file in order to populate the event's parameters with data
    /// </summary>
    /// <param name="ce"></param>
    /// <param name="reader"></param>
    public void SetParameters(CutsceneEvent ce, XmlReader reader)
    {
        object[] parameters = new object[2] { ce, reader };
        InvokeMethod(ce.FunctionName, "SetParameters", parameters);
    }

    public void SetMetaData(CutsceneEvent ce, object metaData)
    {
        object[] parameters = new object[1] { metaData };
        InvokeMethod(ce.FunctionName, "SetMetaData", parameters);
    }

    public void UseParamDefaultValue(CutsceneEvent ce, CutsceneEventParam param)
    {
        object[] parameters = new object[2] { ce, param };
        InvokeMethod(ce.FunctionName, "UseParamDefaultValue", parameters);
    }

    /// <summary>
    /// Instatiates the event invoker based off the method name
    /// </summary>
    /// <param name="eventMethodName"></param>
    /// <returns></returns>
    public ICutsceneEventInterface CreateCutsceneEventInterfaceFromMethod(string eventMethodName)
    {
        MethodInfo method = null;
        ICutsceneEventInterface retVal = CreateInterfaceFromMethod(eventMethodName, "IsFireAndForget", 0, ref method);
        return retVal;
    }

    void InvokeMethod(string eventMethodName, string internalFunctionName, object[] parameters)
    {
        if (m_EventFunctions.Count == 0)
        {
            CheckAvailableEvents();
        }

        MethodInfo method = null;
        ICutsceneEventInterface obj = CreateInterfaceFromMethod(eventMethodName, internalFunctionName, 0, ref method);

        if (obj == null || method == null)
        {
            Debug.LogError($"Failed InvokeEventMethod on {internalFunctionName} (eventMethodName={eventMethodName})");
            return;
        }

        // DEBUG ONLY: log reflection target before invoking.
        //{
        //    string targetType = obj.GetType().FullName;
        //    string declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<unknown>";
        //    string paramDump = FormatParameters(parameters);
        //    Debug.Log(
        //        $"GenericEvents.InvokeMethod() - about to invoke. " +
        //        $"eventMethodName={eventMethodName}, internalFunctionName={internalFunctionName}, " +
        //        $"targetType={targetType}, declaringType={declaringType}, method={method.Name}, " +
        //        $"params={paramDump}"
        //    );
        //}

        try
        {
            method.Invoke(obj, parameters);
        }
        catch (TargetInvocationException tie)
        {
            var root = GetRootException(tie);
            string targetType = obj.GetType().FullName;
            string declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<unknown>";
            string paramDump = FormatParameters(parameters);
            Debug.LogError(
                $"GenericEvents.InvokeMethod() - reflection invoke failed. " +
                $"eventMethodName={eventMethodName}, internalFunctionName={internalFunctionName}, " +
                $"targetType={targetType}, declaringType={declaringType}, method={method.Name}, " +
                $"params={paramDump}, " +
                $"root={(root != null ? (root.GetType().FullName + ": " + root.Message) : "<null>")}, " +
                $"exception={tie}"
            );
            throw;
        }
        catch (Exception e)
        {
            var root = GetRootException(e);
            string targetType = obj.GetType().FullName;
            string declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<unknown>";
            string paramDump = FormatParameters(parameters);
            Debug.LogError(
                $"GenericEvents.InvokeMethod() - invoke failed. " +
                $"eventMethodName={eventMethodName}, internalFunctionName={internalFunctionName}, " +
                $"targetType={targetType}, declaringType={declaringType}, method={method.Name}, " +
                $"params={paramDump}, " +
                $"root={(root != null ? (root.GetType().FullName + ": " + root.Message) : "<null>")}, " +
                $"exception={e}"
            );
            throw;
        }
    }

    T GetReturnValueFromFunction<T>(string eventMethodName, string internalFunctionName, object[] parameters)
    {
        if (m_EventFunctions.Count == 0)
        {
            CheckAvailableEvents();
        }

        MethodInfo method = null;
        ICutsceneEventInterface obj = CreateInterfaceFromMethod(eventMethodName, internalFunctionName, 0, ref method);

        if (obj == null || method == null)
        {
            Debug.LogError($"Failed GetReturnValueFromFunction on {internalFunctionName} (eventMethodName={eventMethodName})");
            return default;
        }

        // DEBUG ONLY: log reflection target before invoking.
        //{
        //    string targetType = obj.GetType().FullName;
        //    string declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<unknown>";
        //    string paramDump = FormatParameters(parameters);
        //    Debug.Log(
        //        $"GenericEvents.GetReturnValueFromFunction() - about to invoke. " +
        //        $"eventMethodName={eventMethodName}, internalFunctionName={internalFunctionName}, " +
        //        $"targetType={targetType}, declaringType={declaringType}, method={method.Name}, " +
        //        $"params={paramDump}"
        //    );
        //}

        try
        {
            object ret = method.Invoke(obj, parameters);

            // DEBUG ONLY: log return value.
            //Debug.Log(
            //    $"GenericEvents.GetReturnValueFromFunction() - invoked. " +
            //    $"eventMethodName={eventMethodName}, internalFunctionName={internalFunctionName}, " +
            //    $"method={method.Name}, returnType={(ret != null ? ret.GetType().FullName : "<null>")}, " +
            //    $"returnValue={SafeToString(ret)}"
            //);

            if (ret == null)
                return default;

            if (ret is T typed)
                return typed;

            // Some code paths might return a boxed value type, or something convertible.
            try
            {
                return (T)Convert.ChangeType(ret, typeof(T));
            }
            catch
            {
                Debug.LogError($"GenericEvents.GetReturnValueFromFunction() - return value is not assignable. expectedType={typeof(T).FullName}, actualType={ret.GetType().FullName}, value={SafeToString(ret)}");
                return default;
            }
        }
        catch (TargetInvocationException tie)
        {
            var root = GetRootException(tie);
            string targetType = obj.GetType().FullName;
            string declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<unknown>";
            string paramDump = FormatParameters(parameters);
            Debug.LogError(
                $"GenericEvents.GetReturnValueFromFunction() - reflection invoke failed. " +
                $"eventMethodName={eventMethodName}, internalFunctionName={internalFunctionName}, " +
                $"targetType={targetType}, declaringType={declaringType}, method={method.Name}, " +
                $"params={paramDump}, " +
                $"root={(root != null ? (root.GetType().FullName + ": " + root.Message) : "<null>")}, " +
                $"exception={tie}"
            );
            throw;
        }
        catch (Exception e)
        {
            var root = GetRootException(e);
            string targetType = obj.GetType().FullName;
            string declaringType = method.DeclaringType != null ? method.DeclaringType.FullName : "<unknown>";
            string paramDump = FormatParameters(parameters);
            Debug.LogError(
                $"GenericEvents.GetReturnValueFromFunction() - invoke failed. " +
                $"eventMethodName={eventMethodName}, internalFunctionName={internalFunctionName}, " +
                $"targetType={targetType}, declaringType={declaringType}, method={method.Name}, " +
                $"params={paramDump}, " +
                $"root={(root != null ? (root.GetType().FullName + ": " + root.Message) : "<null>")}, " +
                $"exception={e}"
            );
            throw;
        }
    }

    /// <summary>
    /// Invokes an event method overload with the given parameters and time
    /// </summary>
    /// <param name="eventMethodName"></param>
    /// <param name="overloadIndex"></param>
    /// <param name="parameters"></param>
    /// <param name="time"></param>
    public void InvokeEventMethod(string eventMethodName, int overloadIndex, object[] parameters, float time, object metaData, MonoBehaviour behaviour)
    {
        MethodInfo method = null;
        ICutsceneEventInterface obj = CreateInterfaceFromMethod(eventMethodName, eventMethodName, overloadIndex, ref method);

        if (obj != null && method != null)
        {
            obj.SetInterpolationTime(time);
            obj.SetMetaData(metaData);
            obj.SetMonoBehaviour(behaviour);
            method.Invoke(obj, parameters);
        }
        else
        {
            Debug.LogError("Failed InvokeEventMethod on " + eventMethodName);
        }
    }

    public ParameterInfo[] GetEventMethodParams(string eventMethodName, int overloadIndex)
    {
        if (m_EventFunctions.Count == 0)
        {
            CheckAvailableEvents();
        }

        List<MethodInfo> methodInfos = m_EventFunctions.FindAll(delegate(MethodInfo info) { return info.Name == eventMethodName; });
        ParameterInfo[] parameters = null;
        if (methodInfos != null && methodInfos.Count > 0)
        {
            if (overloadIndex > methodInfos.Count - 1)
            {
                Debug.Log("bad overload index for function: " + eventMethodName);
                return null;
            }
            parameters = methodInfos[overloadIndex].GetParameters();
        }
        else
        {
            Debug.LogError(string.Format("Couldn't GetEventMethodParams. Method: {0}", eventMethodName));
        }

        return parameters;
    }

    public MethodInfo[] GetEventMethodOverloads(string eventMethodName)
    {
        if (m_EventFunctions.Count == 0)
        {
            CheckAvailableEvents();
        }

        List<MethodInfo> methodInfos = m_EventFunctions.FindAll(delegate(MethodInfo info) { return info.Name == eventMethodName; });
        return methodInfos.ToArray();
    }

    /// <summary>
    /// Creates an ICutsceneEventInterface object which can be used to get and set data of a specific event.
    /// </summary>
    /// <param name="eventMethodName"></param>
    /// <param name="methodToInvokeName"></param>
    /// <param name="methodToInvokeOverloadIndex"></param>
    /// <param name="out_methodToInvoke"></param>
    /// <returns></returns>
    protected ICutsceneEventInterface CreateInterfaceFromMethod(string eventMethodName, string methodToInvokeName, int methodToInvokeOverloadIndex, ref MethodInfo out_methodToInvoke)
    {
#if !UNITY_WSA
        if (m_EventFunctions.Count == 0)
        {
            CheckAvailableEvents();
        }

        // make sure the method exists
        MethodInfo methodInfo = m_EventFunctions.Find(delegate(MethodInfo info) { return info.Name == eventMethodName; });
        if (methodInfo == null)
        {
            Debug.LogError(string.Format("Couldn't CreateInterfaceFromMethod. Method: {0}", eventMethodName));
            return null;
        }

        // search through the nested types
        Type[] nestedTypes = GetType().GetNestedTypes();
        for (int i = 0; i < nestedTypes.Length; i++)
        {
            MethodInfo[] methods = nestedTypes[i].GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance); // only implemented functions
            if (Array.Exists<MethodInfo>(methods, delegate(MethodInfo info) { return info.Name == eventMethodName; }))
            {
                // the method that we want exists, get it and create and object that can invoke it
                out_methodToInvoke = Array.FindAll<MethodInfo>(nestedTypes[i].GetMethods(), delegate(MethodInfo meth) { return meth.Name == methodToInvokeName; })[methodToInvokeOverloadIndex];
                return (ICutsceneEventInterface)Activator.CreateInstance(nestedTypes[i]);
            }
        }

        Debug.LogError(string.Format("Couldn't CreateInterfaceFromMethod. Method: {0}", eventMethodName));
        return null;
#else
        Debug.LogErrorFormat("GenericEvents.CheckAvailableEvents() - not implemented on this platform.");
        return null;
#endif
    }

    private static string SafeToString(object obj)
    {
        if (obj == null)
            return "<null>";

        try
        {
            if (obj is string s)
                return $"\"s\"";

            if (obj is XmlReader xr)
            {
                // Best-effort details without advancing the reader.
                string where = "";
                if (xr is IXmlLineInfo li && li.HasLineInfo())
                    where = $" line={li.LineNumber} pos={li.LinePosition}";
                return $"XmlReader{{name=\"{xr.Name}\"{where}}}";
            }

            return obj.ToString();
        }
        catch
        {
            return "<ToString() threw>";
        }
    }

    private static string FormatParameters(object[] parameters)
    {
        if (parameters == null)
            return "<null>";

        if (parameters.Length == 0)
            return "<none>";

        var sb = new StringBuilder(256);
        for (int i = 0; i < parameters.Length; i++)
        {
            object p = parameters[i];
            string typeName = p != null ? p.GetType().FullName : "<null>";
            sb.Append($"[{i}] {typeName} = {SafeToString(p)}");
            if (i < parameters.Length - 1)
                sb.Append(", ");
        }
        return sb.ToString();
    }

    private static Exception GetRootException(Exception e)
    {
        if (e == null)
            return null;

        while (e.InnerException != null)
            e = e.InnerException;

        return e;
    }

    #endregion
}
}
