using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using TMPro;
using UnityEngine;

public class FileHandler : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField savePathInputField;
    public TMP_InputField loadPathInputField;

    private void Start()
    {
#if UNITY_EDITOR
        // Generate test dialog
        if (!File.Exists("F:/Uni/TestDialog.udsdialog"))
        {
            Dialog dialog = new Dialog("testDialog");

            Dialog.DialogPart diaPart1 = new Dialog.DialogPart("TestID");
            Dialog.DialogPart diaPart2 = new Dialog.DialogPart("Tester2");
            Dialog.DialogPart diaPart3 = new Dialog.DialogPart("TestPart3");

            diaPart2.answers = new Dialog.DialogPart.Answer[]
            {
                new Dialog.DialogPart.Answer("0"),
                new Dialog.DialogPart.Answer("1")
            };

            dialog.startDialogPart = "TestID";
            diaPart1.nextDialogPartID = "Tester2";
            diaPart2.answers[0].nextDialogPartID = "TestPart3";

            dialog.dialogParts = new Dialog.DialogPart[] { diaPart1, diaPart2, diaPart3 };

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream("F:/Uni/TestDialog.udsdialog", FileMode.Create);

            try
            {
                formatter.Serialize(stream, dialog);
            }
            catch (Exception e)
            {
                Debug.Log(e.StackTrace);
            } 
            finally
            {
                stream.Close();
            }
        }
#endif
    }

    public static string BuildDialogFilePath(string nameOrID, string folderPath)
    {
        string path = folderPath;

        path += "\\" + nameOrID;

        if (!nameOrID.EndsWith(".udsdialog"))
            path += ".udsdialog";

        return path;
    }

    /// <summary>
    /// Saves the current Dialog to the path from the save input field.
    /// Calls EditorManager.ConstructDialog() and handles all kinds of 
    /// errors and exceptions internally. The name of the file will be
    /// the dialogID + ".bytes"
    /// </summary>
    public void SaveDialog()
    {
        bool successfulBuild = EditorManager.instance.ConstructDialog();

        if (!successfulBuild)
            return;

        DialogOld dialog = EditorManager.instance.dialog;

        BinaryFormatter formatter = new BinaryFormatter();
        string path = savePathInputField.text;

        if (string.IsNullOrWhiteSpace(path))
        {
            ErrorMessage.instance.ShowErrorMessage("Bitte gib einen Pfad an, du Gurke!");
            return;
        }

        FileStream stream = null;

        try
        {
            DirectoryInfo parent = Directory.GetParent(path);
            if (!parent.Exists)
            {
                ErrorMessage.instance.ShowErrorMessage("Der angegebene Ordner existiert nicht!");
                return;
            }

            //if (!path.EndsWith(".bytes"))
                //path += ".bytes"; // Important for reading in CoT

            // Force filename (= dialogID)
            string filename = "\\" + EditorManager.instance.dialog.id + ".bytes";
            string filenameVar = "/" + EditorManager.instance.dialog.id + ".bytes";
            if (!path.EndsWith(filename) && !path.EndsWith(filenameVar))
            {
                //path = path.Remove(path.LastIndexOf("/") + 1);
                path += filename;
            }

            stream = new FileStream(path, FileMode.Create);

        } 
        catch (DirectoryNotFoundException e)
        {
            ErrorMessage.instance.ShowErrorMessage("Der Ordner wurde nicht gefunden! Überprüfe " +
                "den Pfad nochmal und schau nach, ob er zu einem Ordner führt!");
            Debug.Log(e.ToString());
            return;
        }
        catch (Exception e)
        {
            ErrorMessage.instance.ShowErrorMessage("Der Pfad ist böse! Wahrscheinlich hat das" +
                " Programm keine Permission in diesen Ordner zu schreiben! Nimm einen anderen!");
            Debug.Log(e.ToString());
            return;
        }
        
        try
        {
            formatter.Serialize(stream, dialog);
            ErrorMessage.instance.ShowErrorMessage("Gespeichert!", true);
        }
        catch (Exception e)
        {
            ErrorMessage.instance.ShowErrorMessage(e.ToString());
            Debug.Log(e.ToString());
        }
        finally
        {
            stream.Close();
        }
    }

    /// <summary>
    /// Loads a new dialog from the path from the load input field.
    /// Handles all kinds of errors and exceptions internally. The
    /// file path does not have to end in ".bytes", the method will
    /// account for that internally.
    /// </summary>
    public void LoadDialog ()
    {
        string path = loadPathInputField.text;

        path.Trim();

        if (!path.EndsWith(".bytes"))
            path += ".bytes";

        if (File.Exists(path))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);

            try
            {
                DialogOld data = formatter.Deserialize(stream) as DialogOld;
                EditorManager.instance.LoadDialog(data);

                savePathInputField.text = path.Trim();
                loadPathInputField.text = "";
            }
            finally
            {
                stream.Close();
            }
        }
        else
        {
            ErrorMessage.instance.ShowErrorMessage("Pfad nicht gefunden! " +
                "Bitte nochmal überprüfen!");
        }
    }

    /// <summary>
    /// Checks whether or not a file already exists 
    /// at the save path from the save input field.
    /// Does not return anything but shows the 
    /// "Do you want to override" dialog if true and
    /// just directly saves the dialog if false.
    /// </summary>
    public void CheckIfFileExists()
    {
        string path = savePathInputField.text;

        if (!path.EndsWith(".bytes"))
            path += ".bytes";

        if (File.Exists(path))
            EditorManager.instance.ShowSaveDialog();
        else
            SaveDialog();
    }
}
