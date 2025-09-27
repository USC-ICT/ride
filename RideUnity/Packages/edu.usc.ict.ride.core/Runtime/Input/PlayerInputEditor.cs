using System;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Ride.IO;

static class PlayerInputEditor
{
    static ListRequest listRequest;
    static AddRequest addRequest;
    static string inputSystemPackageName = "com.unity.inputsystem";

    [MenuItem("Ride/Player Input/Setup Player Input Package")]
    public static void SetupPlayerInputPackage()
    {
        listRequest = Client.List();    // List packages installed for the Project
        EditorApplication.update += CheckForInputPackage;
    }

    static void CheckForInputPackage()
    {
        if (listRequest.IsCompleted)
        {
            if (listRequest.Status == StatusCode.Success)
                foreach (var package in listRequest.Result)
                {
                    if (package.name == inputSystemPackageName)
                    {
                        //Debug.Log("Input Package found: " + package.name);

                        EditorApplication.update -= CheckForInputPackage;
                        return;
                    }
                }
            else if (listRequest.Status >= StatusCode.Failure)
                Debug.Log(listRequest.Error.message);

            InstallPackage(inputSystemPackageName);

            EditorApplication.update -= CheckForInputPackage;
        }
    }

    static void InstallPackage(string pkg)
    {
        addRequest = Client.Add(pkg);
        EditorApplication.update += Installation;
    }

    static void Installation()
    {
        if(addRequest.IsCompleted)
        {
            if (listRequest.Status == StatusCode.Success)
                Debug.Log("Input Package successfully installed!");
            else if (listRequest.Status == StatusCode.Failure)
                Debug.LogError("Input Package NOT successfully installed!");

            EditorApplication.update -= Installation;
        }
    }

#if ENABLE_INPUT_SYSTEM
    [MenuItem("Ride/Player Input/Save out default bindings")]
    static void SaveOutDefaultBindings()
    {
        PlayerInputControllerOld playerInputController = UnityEngine.Object.FindFirstObjectByType<PlayerInputControllerOld>();
        if (playerInputController != null)
        {
            PlayerInput playerInput = playerInputController.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                string dirPath = GetDirectoryPath(playerInputController.bindingListDirectory);
                if (dirPath != string.Empty)
                {
                    string jsonContents = playerInput.actions.ToJson();
                    File.WriteAllText(dirPath + "/default.bindings", jsonContents);
                    Debug.Log("Default bindings successfully saved");
                }
                else
                    Debug.LogWarning("Failed to save out default bindings: Binding List Directory does not exist.");
            }
            else
                Debug.LogWarning("Failed to save out default bindings: Player Input Controller does not have Player Input component.");
        }
        else
            Debug.LogWarning("Failed to save out default bindings: Player Input Controller does not exist in the scene.");
    }

    static string GetDirectoryPath(string directory)
    {
        string directoryPath = string.Empty;
        if (Directory.Exists(directory))
            directoryPath = directory;
        else if (Directory.Exists(Application.dataPath + directory))
            directoryPath = Application.dataPath + directory;
        else if (Directory.Exists(Application.persistentDataPath + directory))
            directoryPath = Application.persistentDataPath + directory;

        return directoryPath;
    }
#endif
}
#endif
