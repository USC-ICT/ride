using System;
using System.Collections;
using System.IO;

namespace VHAssets
{
public class IniParser
{
    Hashtable keyPairs = new Hashtable();
    string iniFilePath;

    struct SectionPair
    {
        public string Section;
        public string Key;
    }

    /// <summary>
    /// Opens the INI file at the given path and enumerates the values in the IniParser.
    /// </summary>
    /// <param name="iniPath">Full path to INI file.</param>
    public IniParser(string iniPath)
    {
        TextReader iniFile = null;
        string strLine = null;
        string currentRoot = null;
        string [] keyPair = null;

        iniFilePath = iniPath;

        var www = VHFile.LoadStreamingAssets(iniPath);

        {
            try
            {
                iniFile = new StringReader(www.downloadHandler.text);

                strLine = iniFile.ReadLine();

                while (strLine != null)
                {
                    strLine = strLine.Trim();//.ToUpper();

                    if (strLine != "")
                    {
                        if (strLine.StartsWith("[") && strLine.EndsWith("]"))
                        {
                            strLine.ToUpper();//new
                            currentRoot = strLine.Substring(1, strLine.Length - 2);
                        }
                        else
                        {
                            if (strLine.StartsWith(";") || strLine.StartsWith("#"))
                            {
                                // it's a comment, do nothing
                            }
                            else
                            {
                                keyPair = strLine.Split(new char[] { '=' }, 2);

                                SectionPair sectionPair;
                                string value = null;

                                if (currentRoot == null)
                                    currentRoot = "ROOT";

                                sectionPair.Section = currentRoot.ToUpper();
                                sectionPair.Key = keyPair[0].ToUpper();

                                if (keyPair.Length > 1)
                                    value = keyPair[1];

                                sectionPair.Key = sectionPair.Key.Trim();
                                value = value.Trim();

                                //UnityEngine.Debug.Log("sectionPair.Section: " + sectionPair.Section + " sectionPair.Key: " + sectionPair.Key
                                //     + " value: " + value);

                                keyPairs.Add(sectionPair, value);
                            }
                        }
                    }

                    strLine = iniFile.ReadLine();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
#if !UNITY_WSA
                iniFile?.Close();
#endif
            }
        }
    }

    /// <summary>
    /// Returns the value for the given section, key pair.
    /// </summary>
    /// <param name="sectionName">Section name.</param>
    /// <param name="settingName">Key name.</param>
    public string GetSetting(string sectionName, string settingName)
    {
        SectionPair sectionPair;
        sectionPair.Section = sectionName.ToUpper();
        sectionPair.Key = settingName.ToUpper();

        return (string)keyPairs[sectionPair];
    }

    /// <summary>
    /// Returns true if the section and name exists, false otherwise
    /// </summary>
    /// <param name="sectionName">Section name.</param>
    /// <param name="settingName">Key name.</param>
    /// <returns></returns>
    public bool SettingExists(string sectionName, string settingName)
    {
        SectionPair sectionPair;
        sectionPair.Section = sectionName.ToUpper();
        sectionPair.Key = settingName.ToUpper();

        return keyPairs.Contains(sectionPair);
    }

    /// <summary>
    /// Enumerates all lines for given section.
    /// </summary>
    /// <param name="sectionName">Section to enum.</param>
    public string [] EnumSection(string sectionName)
    {
        ArrayList tmpArray = new ArrayList();

        foreach (SectionPair pair in keyPairs.Keys)
        {
            if (pair.Section == sectionName.ToUpper())
                tmpArray.Add(pair.Key);
        }

        return (string [])tmpArray.ToArray(typeof(string));
    }

    /// <summary>
    /// Adds or replaces a setting to the table to be saved.
    /// </summary>
    /// <param name="sectionName">Section to add under.</param>
    /// <param name="settingName">Key name to add.</param>
    /// <param name="settingValue">Value of key.</param>
    public void AddSetting(string sectionName, string settingName, string settingValue)
    {
        SectionPair sectionPair;
        sectionPair.Section = sectionName.ToUpper();
        sectionPair.Key = settingName.ToUpper();

        if (keyPairs.ContainsKey(sectionPair))
            keyPairs.Remove(sectionPair);

        keyPairs.Add(sectionPair, settingValue);
    }

    /// <summary>
    /// Adds or replaces a setting to the table to be saved with a null value.
    /// </summary>
    /// <param name="sectionName">Section to add under.</param>
    /// <param name="settingName">Key name to add.</param>
    public void AddSetting(string sectionName, string settingName)
    {
        AddSetting(sectionName, settingName, null);
    }

    /// <summary>
    /// Remove a setting.
    /// </summary>
    /// <param name="sectionName">Section to add under.</param>
    /// <param name="settingName">Key name to add.</param>
    public void DeleteSetting(string sectionName, string settingName)
    {
        SectionPair sectionPair;
        sectionPair.Section = sectionName.ToUpper();
        sectionPair.Key = settingName.ToUpper();

        if (keyPairs.ContainsKey(sectionPair))
            keyPairs.Remove(sectionPair);
    }

    /// <summary>
    /// Save settings to new file.
    /// </summary>
    /// <param name="newFilePath">New file path.</param>
    public void SaveSettings(string newFilePath)
    {
        ArrayList sections = new ArrayList();
        string tmpValue = "";
        string strToSave = "";

        foreach (SectionPair sectionPair in keyPairs.Keys)
        {
            if (!sections.Contains(sectionPair.Section))
                sections.Add(sectionPair.Section);
        }

        foreach (string section in sections)
        {
            strToSave += ("[" + section + "]\r\n");

            foreach (SectionPair sectionPair in keyPairs.Keys)
            {
                if (sectionPair.Section == section)
                {
                    tmpValue = (string)keyPairs[sectionPair];

                    if (tmpValue != null)
                        tmpValue = "=" + tmpValue;

                    strToSave += (sectionPair.Key + tmpValue + "\r\n");
                }
            }

            strToSave += "\r\n";
        }

        try
        {
#if !UNITY_WSA
            string newFilePathFull = VHFile.GetStreamingAssetsPath() + newFilePath;
            TextWriter tw = new StreamWriter(newFilePathFull);
            tw.Write(strToSave);
            tw.Close();
#endif
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    /// <summary>
    /// Save settings back to ini file.
    /// </summary>
    public void SaveSettings()
    {
        SaveSettings(iniFilePath);
    }
}
}
