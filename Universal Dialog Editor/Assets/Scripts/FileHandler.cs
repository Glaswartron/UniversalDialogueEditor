using System;
using System.IO;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FileHandler
{
    [Header("UI")]
    public TMP_InputField savePathInputField;
    public TMP_InputField loadPathInputField;

    private static readonly string GLOBAL_PROPERTIES_PATH
        = Path.Combine(Application.persistentDataPath, "globalProperties.udsgp.json");

    private static readonly string DIALOG_PART_PROPERTY_PRESET_PATH
        = Path.Combine(Application.persistentDataPath, "PropertyPresets", "DialogPartPropertyPresets");

    private static readonly string ANSWER_PROPERTY_PRESET_PATH
        = Path.Combine(Application.persistentDataPath, "PropertyPresets", "AnswerPropertyPresets");

    public static void CreateTestDialog()
    {
#if UNITY_EDITOR
        // Generate test dialog
        if (!File.Exists("F:/Testground/TestDialog.udsdialog.json"))
        {
            Dialog dialog = new Dialog("TestDialog");

            Dialog.DialogPart diaPart1 = new Dialog.DialogPart("TestID", new Vector2(0, 0));
            Dialog.DialogPart diaPart2 = new Dialog.DialogPart("Tester2", new Vector2(0, 1));
            Dialog.DialogPart diaPart3 = new Dialog.DialogPart("TestPart3", new Vector2(1, 0));

            diaPart2.answers = new Dialog.DialogPart.Answer[]
            {
                new Dialog.DialogPart.Answer("Yes", 0, Mathf.PI),
                new Dialog.DialogPart.Answer("No", 1, Mathf.PI/2)
            };

            dialog.startDialogPartID = "TestID";
            diaPart1.nextDialogPartID = "Tester2";
            diaPart2.answers[0].nextDialogPartID = "TestPart3";

            diaPart1.SetProperty("tollerSchluessel", "Hier koennte ihre Werbung");
            diaPart2.SetProperty("jetztAuchMitZahlenLol", 42);

            dialog.dialogParts = new Dialog.DialogPart[] { diaPart1, diaPart2, diaPart3 };

            FileStream stream = new FileStream("F:/Testground/TestDialog.udsdialog.json", FileMode.Create);

            string dialogJSON = ToJSON(dialog);

            StreamWriter writer = null;

            try
            {
                writer = new StreamWriter(stream);
                writer.Write(dialogJSON);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                writer?.Flush();
                writer?.Close();
            }
        }
#endif
    }

    /// <summary>
    /// Gets the paths to all dialogs (.udsdialog.json) in the given directory
    /// </summary>
    /// <param name="dirPath">The path to the directory</param>
    /// <returns>A string array containing the file paths of all .udsdialog.json files
    /// in the given directory or an empty array if the folder is empty
    /// or something went wrong</returns>
    public static string[] GetAllDialogPathsFromDir(string dirPath)
    {
        if (string.IsNullOrWhiteSpace(dirPath))
            return new string[0];

        // Load folder content (get files as string[] of their paths)
        if (Directory.Exists(dirPath))
            try
            {

                return Directory.GetFiles(dirPath, "*.udsdialog.json");

            }
            catch (Exception e)
            {
                ErrorMessage.instance.ShowErrorMessage
                    ("An error occured while loading dialogs from a directory.");

                Debug.LogError(e.Message);

                return new string[0];
            }
        else
        {
            ErrorMessage.instance.ShowErrorMessage
                ("The directory path is invalid.");
            return new string[0];
        }
    }

    /// <summary>
    /// Serializes the given dialog into a new .udsdialog.json
    /// file in the given folder path. 
    /// The name/path to the file follows the following rule:
    /// .../.../nameOrID.udsdialog.json
    /// </summary>
    /// <param name="dialog">The dialog to be saved</param>
    /// <param name="folderPath">The path to the folder where the dialog shall be saved</param>
    /// <returns>The path to the newly created file - null if something went wrong</returns>
    public static string CreateNewDialogFile(Dialog dialog, string folderPath)
    {
        string path = BuildDialogFilePath(dialog.id, folderPath);

        if (!File.Exists(path))
        {
            FileStream stream = null;
            StreamWriter writer = null;

            try
            {
                stream = new FileStream(path, FileMode.Create);

                writer = new StreamWriter(stream);

                string dialogJSON = ToJSON(dialog);
                writer.Write(dialogJSON);
            }
            catch (Exception e)
            {
                ErrorMessage.instance.ShowErrorMessage
                    ("Something went wrong while creating the file. Please " +
                    "check the dialog id you entered. " +
                    "Also check if you/the editor have/has " +
                    "writing permission for the selected folder. " +
                    "Try changing the folder");

                Debug.LogError(e.Message);

                return null;
            }
            finally
            {
                writer?.Flush();
                writer?.Close();
            }
        }
        else
        {
            ErrorMessage.instance.ShowErrorMessage
                ("A dialog with this id/name (path!) already exists in this folder!");

            return null;
        }

        return path;
    }

    /// <summary>
    /// Loads/Deserializes a dialog from the .udsdialog.json 
    /// bytes-file at the given path. Returns null if
    /// something went wrong.
    /// </summary>
    /// <param name="path">The path to the dialog file</param>
    /// <returns>The deserialized Dialog as a dialog object - null if
    /// something went wrong</returns>
    public static Dialog LoadDialogFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) // Shouldn't happen
        {
            Debug.LogError("The path passed to FileHandler.LoadDialogFile " +
                "was either null or white space");
            return null;
        }

        if (File.Exists(path))
        {
            FileStream stream = null;
            StreamReader reader = null;

            try
            {
                stream = new FileStream(path, FileMode.Open);

                reader = new StreamReader(stream);
                string dialogJSON = reader.ReadToEnd();

                return JsonConvert.DeserializeObject<Dialog>(dialogJSON);
            }
            catch (Exception e)
            {
                ErrorMessage.instance.ShowErrorMessage("An error occured while loading " +
                    "the dialog. The file might be corrupted or the JSON cannot be parsed");
                Debug.LogError("Path: " + path + " -- " + e.Message);
                return null;
            }
            finally
            {
                reader?.Close();
            }
        }
        else
        {
            ErrorMessage.instance.ShowErrorMessage("File not found");
            Debug.LogError(path + " doesn't exist?");
            return null;
        }
    }


    /// <summary>
    /// Serializes the given dialog into an existing/its .udsdialog.json 
    /// file at the given path, thus overriding the old file and saving
    /// the dialog. Also renames the file if the DialogID has changed.
    /// The name/path to the file must follow the following rule:
    /// .../.../nameOrID.udsdialog.json
    /// </summary>
    /// <param name="dialog">The dialog to be saved</param>
    /// <param name="folderPath">The path to the .udsdialog.json file which shall be overridden</param>
    /// <returns>Successful?</returns>
    public static bool SaveDialog(Dialog dialog, string path)
    {
        //string path = BuildDialogFilePath(dialog.id, folderPath);

        if (!File.Exists(path))
        {
            // Shouldn't happen - use FileHandler.CreateNewDialogFile instead
            Debug.LogWarning("FileHandler.SaveDialogFile was called with path " +
                path + " although there is not yet an existing dialog file in that location");
        }

        string dir = Path.GetDirectoryName(path);
        string newPath = BuildDialogFilePath(dialog.id, dir);

        bool useNewPath = false;
        bool pathsEqualExceptForCase = false;

        /* More testing required. Note that on Windows
           paths are case insensitive, on Linux they aren't
           (on MacOS they are?) */
        if (!newPath.Equals(path))
        {
            useNewPath = true; // DialogID has changed
            if (newPath.ToLower().Equals(path.ToLower()))
                pathsEqualExceptForCase = true;
        }

        string actualPath = useNewPath ? newPath : path;

        //BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = null;
        StreamWriter writer = null;

        try
        {
            stream = new FileStream(actualPath, FileMode.Create);

            string dialogJSON = ToJSON(dialog);

            writer = new StreamWriter(stream);
            writer.Write(dialogJSON);
        }
        catch (Exception e)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Something went wrong while saving the file. Please " +
                "check if the dialog name/id you entered contains any " +
                "invalid characters. Also check if you/the editor has " +
                "writing permission for the folder you selected. " +
                "Try changing it to a different folder");

            Debug.LogError(e.Message);

            return false;
        }
        finally
        {
            writer?.Flush();
            writer?.Close();
        }

        /* Delete the old file after the new one has been written successfully.
         * Only do so if the paths are really different OR on Linux
         * (where paths are case insensitive) */
        if (useNewPath &&
            (!pathsEqualExceptForCase || Application.platform == RuntimePlatform.LinuxPlayer))
            DeleteDialogFile(path);

        return true;
    }

    /// <summary>
    /// Deletes the file at the given file path.
    /// Basically a wrapper for File.Delete(path)
    /// </summary>
    /// <param name="path">The path to the file that shall be deleted</param>
    /// <returns>Successful?</returns>
    public static bool DeleteDialogFile(string path)
    {
        try
        {
            File.Delete(path);
            return !File.Exists(path);
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return false;
        }
    }

    public static bool SaveGlobalProperties()
    {
        Dictionary<string, UDSProperty> properties = EditorManager.globalProperties;

        FileStream stream = null;
        StreamWriter writer = null;

        try
        {
            stream = new FileStream(GLOBAL_PROPERTIES_PATH, FileMode.Create);
            writer = new StreamWriter(stream);

            string propertiesJSON = ToJSON(properties);
            writer.Write(propertiesJSON);
        }
        catch (Exception e)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Something went wrong while saving the Global Properties under " +
                GLOBAL_PROPERTIES_PATH + ". " +
                "Please check if the path still exists and if there is enough disk space");

            Debug.LogError(e.Message);

            return false;
        }
        finally
        {
            writer?.Flush();
            writer?.Close();
        }

        return true;
    }

    public static Dictionary<string, UDSProperty> LoadGlobalProperties()
    {
        if (!File.Exists(GLOBAL_PROPERTIES_PATH))
            return null;

        FileStream stream = null;
        StreamReader reader = null;

        try
        {
            stream = new FileStream(GLOBAL_PROPERTIES_PATH, FileMode.Open);
            reader = new StreamReader(stream);

            string propertiesJSON = reader.ReadToEnd();
            Dictionary<string, UDSProperty> properties 
                = JsonConvert.DeserializeObject<Dictionary<string, UDSProperty>>(propertiesJSON);

            return properties;
        }
        catch (Exception e)
        {
            ErrorMessage.instance.ShowErrorMessage("An error occured while loading the " +
                "Global Properties from " + GLOBAL_PROPERTIES_PATH + ". Please check " +
                "if the path is still valid");
            Debug.LogError("Path: " + GLOBAL_PROPERTIES_PATH + " -- " + e.Message);
            return null;
        }
        finally
        {
            reader?.Close();
        }
    }

    public static bool ExistsPropertyPreset(PropertyPreset propertyPreset, string path = null)
    {
        if (path == null)
        {
            path = GetPropertyPresetDirectoryPath(propertyPreset.propertyPresetType);
            path = Path.Combine(path, propertyPreset.id + ".udspreset.json");
        }

        return File.Exists(path);
    }

    public static bool SavePropertyPreset(PropertyPreset propertyPreset, string path = null)
    {
        CreateDirectoriesIfNotThere();

        if (path == null)
        {
            path = GetPropertyPresetDirectoryPath(propertyPreset.propertyPresetType);
            path = Path.Combine(path, propertyPreset.id + ".udspreset.json");
        }

        FileStream stream = null;
        StreamWriter writer = null;

        try
        {
            stream = new FileStream(path, FileMode.Create);
            writer = new StreamWriter(stream);

            string presetJSON = ToJSON(propertyPreset);
            writer.Write(presetJSON);
        }
        catch (Exception e)
        {
            ErrorMessage.instance.ShowErrorMessage
                ("Something went wrong while saving the Property Preset under" + path + ". " +
                "Please check if the name is valid, the path exists and if there is enough disk space");

            Debug.LogError(e.Message);

            return false;
        }
        finally
        {
            writer?.Flush();
            writer?.Close();
        }

        return true;
    }

    public static string[] GetAllPropertyPresetIDs(PropertyPreset.PropertyPresetType type)
    {
        CreateDirectoriesIfNotThere();

        string path = GetPropertyPresetDirectoryPath(type);

        if (path == null)
        {
            Debug.LogError("Invalid path in GetAllPropertyPresetIDs");
            return null;
        }

        string[] paths = Directory.GetFiles(path, "*.udspreset.json");
        
        return Array.ConvertAll(paths, p => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(p)));
    }

    public static PropertyPreset? LoadPropertyPreset
        (string id, PropertyPreset.PropertyPresetType type, string path = null)
    {
        if (path == null)
        {
            path = GetPropertyPresetDirectoryPath(type);
            path = Path.Combine(path, id + ".udspreset.json");
        }

        FileStream stream = null;
        StreamReader reader = null;

        try
        {

            stream = new FileStream(path, FileMode.Open);
            reader = new StreamReader(stream);

            string presetJSON = reader.ReadToEnd();
            PropertyPreset preset = JsonConvert.DeserializeObject<PropertyPreset>(presetJSON);

            return preset;
        }
        catch (Exception e)
        {
            ErrorMessage.instance.ShowErrorMessage("An error occured while loading a " +
                "Property Preset from " + path + ". Please check if the path is still valid");

            Debug.LogError("Path: " + path + " -- " + e.Message);

            return null;
        }
        finally
        {
            reader?.Close();
        }
    }

    public static void ImportPropertyPreset(string path)
    {
        PropertyPreset? preset = LoadPropertyPreset(null, default, path);

        if (preset == null)
        {
            Debug.LogError("Error while importing Property Preset from path " + path);
            return;
        }

        path = GetPropertyPresetDirectoryPath(preset.Value.propertyPresetType);
        path = Path.Combine(path, preset.Value.id + ".udspreset.json");

        SavePropertyPreset(preset.Value, path);
    }

    public static void ExportPropertyPreset
        (string id, PropertyPreset.PropertyPresetType type, string path)
    {
        PropertyPreset? preset = LoadPropertyPreset(id, type, null);

        if (preset == null)
        {
            Debug.LogError("Error while exporting Property Preset to path " + path);
            return;
        }

        SavePropertyPreset(preset.Value, Path.Combine(path, id + ".udspreset.json"));
    }

    /// <summary>
    /// Builds and returns the (hypothetical) path to a
    /// dialog with ID nameOrID in directory folderPath
    /// </summary>
    /// <param name="nameOrID">The ID of the dialog</param>
    /// <param name="folderPath">The path where its file should go</param>
    /// <returns>A fitting save/load path for the dialog (ending in .udsdialog.json)</returns>
    public static string BuildDialogFilePath(string nameOrID, string folderPath)
    {
        string path = Path.Combine(folderPath, nameOrID);

        if (!nameOrID.EndsWith(".udsdialog.json"))
            path += ".udsdialog.json";

        return path;
    }

    public static string GetPropertyPresetDirectoryPath(PropertyPreset.PropertyPresetType type)
    {
        switch (type)
        {
            case PropertyPreset.PropertyPresetType.DIALOG_PART:
                return DIALOG_PART_PROPERTY_PRESET_PATH;
            case PropertyPreset.PropertyPresetType.ANSWER:
                return ANSWER_PROPERTY_PRESET_PATH;
            default:
                return null;
        }
    }

    private static string ToJSON(object obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented,
            new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
    }

    private static bool CreateDirectoriesIfNotThere()
    {
        bool directoryCreated = false;

        if (!Directory.Exists(DIALOG_PART_PROPERTY_PRESET_PATH))
        {
            Directory.CreateDirectory(DIALOG_PART_PROPERTY_PRESET_PATH);
            directoryCreated = true;
        }

        if (!Directory.Exists(ANSWER_PROPERTY_PRESET_PATH))
        {
            Directory.CreateDirectory(ANSWER_PROPERTY_PRESET_PATH);
            directoryCreated = true;
        }

        return directoryCreated;
    }
}
