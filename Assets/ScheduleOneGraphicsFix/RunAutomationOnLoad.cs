// Assets/Editor/RunAutomationOnLoad.cs
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class RunAutomationOnLoad
{
    // Ensure this key is unique enough for your project to avoid conflicts with other EditorPrefs.
    // Appending a hash of the project path makes it unique per project.
    private const string BaseEditorPrefKey = "ScheduleOne_ProjectFixHasRun_";

    static RunAutomationOnLoad()
    {
        // Using delayCall ensures that this runs after the editor is fully initialized
        // and not in the middle of other static constructor chains or compilation.
        EditorApplication.delayCall += OnScriptsReloadedAndEditorReady;
    }

    private static void OnScriptsReloadedAndEditorReady()
    {
        // Create a project-specific key to store the flag
        string projectSpecificRunFlagKey = BaseEditorPrefKey + Application.dataPath.GetHashCode().ToString();

        // Check if the main automation has already run for this project in this editor session/preference set
        if (!EditorPrefs.GetBool(projectSpecificRunFlagKey, false)) // Default to 'false' if the key doesn't exist
        {
            Debug.Log($"[RunAutomationOnLoad] Flag '{projectSpecificRunFlagKey}' is false or not set. Attempting to run ScheduleOneAutomation.FixShadersInAll().");

            try
            {
                // Call your main automation method
                ScheduleOneAutomation.FixShadersInAll();
                Debug.Log("[RunAutomationOnLoad] Successfully called ScheduleOneAutomation.FixShadersInAll().");

                // Set the flag to true so it doesn't run automatically on the next editor load
                EditorPrefs.SetBool(projectSpecificRunFlagKey, true);
                Debug.Log($"[RunAutomationOnLoad] Set flag '{projectSpecificRunFlagKey}' to true. The automation will not run automatically on the next load unless this flag is cleared.");
            }
            catch (System.Exception e)
            {
                // Log any errors that occur during the execution of your main automation method
                Debug.LogError($"[RunAutomationOnLoad] An error occurred while trying to run ScheduleOneAutomation.FixShadersInAll(): {e.Message}\n{e.StackTrace}");
                // Consider whether to set the flag to true even if there's an error,
                // or if you want it to retry on the next load. For now, it won't set the flag on error.
            }
        }
        else
        {
            Debug.Log($"[RunAutomationOnLoad] Flag '{projectSpecificRunFlagKey}' is already true. Skipping automatic run of ScheduleOneAutomation.FixShadersInAll().");
        }
    }

    // Optional: Add a menu item to manually clear the flag if you need to re-run the automation
    [MenuItem("Assets/ScheduleOne/Force Re-run Project Fix on Next Load")]
    public static void ClearHasRunFlag()
    {
        string projectSpecificRunFlagKey = BaseEditorPrefKey + Application.dataPath.GetHashCode().ToString();
        EditorPrefs.DeleteKey(projectSpecificRunFlagKey);
        Debug.Log($"[RunAutomationOnLoad] Cleared flag '{projectSpecificRunFlagKey}'. The full project fix will attempt to run on the next editor load/script reload.");
    }

    // Optional: Add a menu item to manually trigger the fix if needed, separate from the auto-run
    [MenuItem("Assets/ScheduleOne/Manually Trigger Full Project Fix Now")]
    public static void ManuallyTriggerFix()
    {
        Debug.Log("[RunAutomationOnLoad] Manually triggering ScheduleOneAutomation.FixShadersInAll()...");
        try
        {
            ScheduleOneAutomation.FixShadersInAll();
            Debug.Log("[RunAutomationOnLoad] Manual trigger of ScheduleOneAutomation.FixShadersInAll() completed.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[RunAutomationOnLoad] Error during manual trigger of ScheduleOneAutomation.FixShadersInAll(): {e.Message}\n{e.StackTrace}");
        }
    }
}