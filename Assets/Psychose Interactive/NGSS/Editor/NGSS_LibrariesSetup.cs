#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;
using System.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace PI.NGSS
{
    public class NGSS_LibrariesSetup : ScriptableWizard
    {
        string NGSSAssetPath = "/Psychose Interactive";
        string installer_filename = null;
        string cgIncludesPath = null;
        string installLog = null;
        bool installLocalLibrary = false;
        bool localLibrary = false;
        bool installDirectionalLibrary = false;
        bool directionalLibrary = false;
        bool displayRestart = false;
        bool isInit = false;

        //string downloadmanURL = "https://cdn.discordapp.com/attachments/552191621026938902/722739017170550814/NGSS_Installer.manifest.txt";   //raw discord
        //string downloadexeURL = "https://cdn.discordapp.com/attachments/552191621026938902/722735604949057557/NGSS_Installer.txt";            //raw discord
        string downloadmanURL = "https://raw.githubusercontent.com/tatoforever/NGSS/master/NGSS_Installer.manifest.txt"; //raw github
        string downloadexeURL = "https://raw.githubusercontent.com/tatoforever/NGSS/master/NGSS_Installer.txt"; //raw github

        float processingFileProgress = 0f;

        //Text textDownload;
        Image imageProgress;
        bool initDownload = false;
        bool startDownload = false;
        UnityWebRequest uwr1;
        UnityWebRequest uwr2;

        UnityWebRequest DownloadFile(string URL, string localPath, float scaleProgress, bool showProgress)
        {
            UnityWebRequest uwr = new UnityWebRequest(URL, UnityWebRequest.kHttpVerbGET);
            //UnityWebRequest uwr = new UnityWebRequest(GenerateRequestURL(URL, null, "GET"));

            //uwr.useHttpContinue = false;
            //uwr.chunkedTransfer = false;
            //uwr.redirectLimit = 0;  // disable redirects
            //uwr.timeout = 60;       // don't make this small, web requests do take some time
            uwr.downloadHandler = new DownloadHandlerFile(localPath);

            uwr.SendWebRequest();

            return uwr;
        }

        [MenuItem("Tools/Psychose Interactive/NGSS Libraries Setup (Built-in Renderer)")]
        public static void WizardSetups()
        {
            DisplayWizard("NGSS Libraries Setup", typeof(NGSS_LibrariesSetup), "LibrariesSetup");
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
#if UNITY_5
        minSize = new Vector2(350, 350);
        maxSize = new Vector2(350, 350);
#else
            minSize = new Vector2(350, 350);
            maxSize = new Vector2(350, 350);
#endif
            if (!isInit)
            {
                try
                {
                    var entryAssembly = new StackTrace().GetFrames().Last().GetMethod().Module.Assembly;
                    var managedDir = Path.GetDirectoryName(entryAssembly.Location);
                    //UnityEngine.Debug.Log(Path.Combine(Directory.GetParent(managedDir).Parent.FullName, "CGIncludes"));
#if UNITY_5
                cgIncludesPath = managedDir + "/../CGIncludes/";
#else
                    cgIncludesPath = Path.Combine(Directory.GetParent(managedDir).Parent.FullName, "CGIncludes");
#endif
                    if (!Directory.Exists(cgIncludesPath))
                    {
#if UNITY_5
                    cgIncludesPath = managedDir + "/../../CGIncludes/";
#else
                        cgIncludesPath = Path.Combine(Directory.GetParent(managedDir).Parent.Parent.FullName, "CGIncludes");
#endif
                    }

                    if (!Directory.Exists(cgIncludesPath))
                    {
                        UnityEngine.Debug.LogError("Can't find directory: " + cgIncludesPath + ". Please proceed to the manual installation.");
                        return;
                    }

                    UnityEngine.Debug.Log("Current CGIncludes path: " + cgIncludesPath);
                    UnityEngine.Debug.Log("Current Editor Assets Path: " + Application.dataPath);
                    UnityEngine.Debug.Log("Current NGSS Assets Path: " + Application.dataPath + NGSSAssetPath);

                    //initialize button toggle states based on bak file exist
                    installLocalLibrary = File.Exists(Path.Combine(cgIncludesPath, "UnityShadowLibrary.cginc.bak"));
                    localLibrary = installLocalLibrary;

                    installDirectionalLibrary = File.Exists(Path.Combine(cgIncludesPath, "AutoLight.cginc.bak")) && File.Exists(Path.Combine(cgIncludesPath, "UnityDeferredLibrary.cginc.bak"));
                    directionalLibrary = installDirectionalLibrary;
#if !UNITY_5
                    if (Application.platform == RuntimePlatform.WindowsEditor && Directory.Exists(cgIncludesPath))
                    {
                        Process.Start("explorer.exe", cgIncludesPath);
                    }
#endif
                    UnityEngine.Debug.Log(Path.GetFullPath(Application.dataPath + NGSSAssetPath));

                    isInit = true;
                }
                catch (System.Exception ex)
                {
                    GUI.Label(new Rect(10, 15, 330, 75), ex.Message);
                    UnityEngine.Debug.LogWarning(ex.Message);
                    return;
                }
            }

            /********************************************************/

            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                if (initDownload == false)
                {
                    if (File.Exists(Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe.manifest") == false || File.Exists(Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe") == false)
                    {
                        uwr1 = DownloadFile(downloadmanURL, Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe.manifest", 1f, false);
                        uwr2 = DownloadFile(downloadexeURL, Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe", 1f, false);
                        startDownload = true;
                    }

                    initDownload = true;
                }

                if (startDownload)
                {
                    if (uwr1.isDone == false || uwr2.isDone == false)
                    {
                        processingFileProgress = Mathf.Clamp01(uwr1.downloadProgress * 0.5f + uwr2.downloadProgress * 0.5f);
                        //if (showProgress && imageProgress) { imageProgress.fillAmount = processingFileProgress; }
                        return;
                    }

                    if (uwr1.isNetworkError || uwr1.isHttpError)
                        UnityEngine.Debug.LogError(uwr1.error);

                    if (uwr2.isNetworkError || uwr2.isHttpError)
                        UnityEngine.Debug.LogError(uwr2.error);

                    uwr1.Dispose();
                    uwr2.Dispose();
                    startDownload = false;
                }
            }

            /********************************************************/

            //GUIStyle mystyle = new GUIStyle("style string name");
            if (displayRestart)
                GUI.Label(new Rect(20, 15, 310, 85), installLog, "TextArea");
            else
                GUI.Label(new Rect(20, 15, 310, 85), "This Wizard assist you with the installation and removal of NGSS internal libraries.\nNote: You need to install NGSS libraries using this tool every time you install or upgrade Unity and/or NGSS.", "TextArea");

            GUI.BeginGroup(new Rect(10, 80, 330, 325), "");

            GUI.Label(new Rect(10, 30, 310, 25), "NGSS Assets relative path:");

            NGSSAssetPath = GUI.TextField(new Rect(10, 60, 310, 25), NGSSAssetPath);

            if (isInit)
            {
                installLocalLibrary = GUI.Toggle(new Rect(10, 90, 310, 50), installLocalLibrary, !installLocalLibrary ? "Install NGSS Spot/Point libraries" : "Uninstall NGSS Spot/Point libraries", "Button");
                installDirectionalLibrary = GUI.Toggle(new Rect(10, 150, 310, 50), installDirectionalLibrary, !installDirectionalLibrary ? "Install NGSS Directional libraries" : "Uninstall NGSS Directional libraries", "Button");
#if !UNITY_5
                Rect shaderCacheButtonRect = new Rect(10, 210, 310, 50); //if ShadowBias is not available, move Shader Cache button up
#else
            Rect shaderCacheButtonRect = new Rect(10, 210, 310, 50);
#endif
                if (GUI.Button(shaderCacheButtonRect, "Delete Project ShaderCache folder", "Button"))
                {
                    if (Directory.Exists(Application.dataPath + "/../Library/ShaderCache"))
                    {
                        if (Application.platform == RuntimePlatform.WindowsEditor)
                        {
                            installer_filename = Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe";
                            var proc = Process.Start(installer_filename, "DELETE_CACHE " + "\"" + Directory.GetParent(Application.dataPath).FullName + " /Library/ShaderCache\"");
                            //kill the proccess (no need, it kills itself)
                            //proc.CloseMainWindow();
                            //proc.Close();
                        }
                        else
                        {
                            Directory.Delete(Application.dataPath + "/../Library/ShaderCache", true);
                        }

                        installLog = "ShaderCache folder successfully deleted. Please restart the Editor to rebuild project ShaderCache.";
                        UnityEngine.Debug.Log(installLog);
                    }
                    else
                    {
                        installLog = "ShaderCache folder not found. Please restart the Editor to rebuild project ShaderCache.";
                        UnityEngine.Debug.Log(installLog);
                    }
                }

                if (localLibrary != installLocalLibrary)
                {
                    try
                    {
                        displayRestart = true;
                        localLibrary = installLocalLibrary;
                        if (installLocalLibrary) //install it
                        {
                            if (GUI.changed && string.IsNullOrEmpty(NGSSAssetPath) == false && (NGSSAssetPath[NGSSAssetPath.Length - 1] == '/' || NGSSAssetPath[NGSSAssetPath.Length - 1].ToString() == @"\"))
                            {
                                NGSSAssetPath = NGSSAssetPath.Remove(NGSSAssetPath.Length - 1);
                            }

                            if (File.Exists(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityShadowLibrary.cginc"))
                            {
                                if (Application.platform == RuntimePlatform.WindowsEditor)
                                {
                                    installer_filename = Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe";
                                    var proc = Process.Start(installer_filename, "INSTALL_LOCAL " + "\"" + Application.dataPath + NGSSAssetPath + "\"" + " " + "\"" + cgIncludesPath + "\"");
                                }
                                else
                                {
                                    //File.Replace(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc.bak");
                                    File.Move(cgIncludesPath + "/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc.bak"); //rename
                                    File.Copy(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityShadowLibrary.cginc", cgIncludesPath + "/UnityShadowLibrary.cginc", true); //copy
                                }

                                installLog = "NGSS Local libraries successfully installed. Unity Local libraries have been backed up. Make sure your scene lights have it's corresponding NGSS components.\nPlease restart the Editor to apply changes.";
                                UnityEngine.Debug.Log(installLog);
                            }
                            else
                            {
                                //GUI.Label(new Rect(10, 15, 330, 75), Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityShadowLibrary.cginc does not exist make sure the file exist!");
                                installLog = Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityShadowLibrary.cginc does not exist make sure the file exist!";
                                UnityEngine.Debug.LogWarning(installLog);
                            }
                        }
                        else //revert it
                        {
                            if (GUI.changed && string.IsNullOrEmpty(NGSSAssetPath) == false && (NGSSAssetPath[NGSSAssetPath.Length - 1] == '/' || NGSSAssetPath[NGSSAssetPath.Length - 1].ToString() == @"\"))
                            {
                                NGSSAssetPath = NGSSAssetPath.Remove(NGSSAssetPath.Length - 1);
                            }

                            if (Application.platform == RuntimePlatform.WindowsEditor)
                            {
                                installer_filename = Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe";
                                var proc = Process.Start(installer_filename, "DELETE_LOCAL " + "\"" + Application.dataPath + NGSSAssetPath + "\"" + " " + "\"" + cgIncludesPath + "\"");
                            }
                            else
                            {
                                //UnityShadowLibrary
                                string shadowLibraryPath = Path.Combine(cgIncludesPath, "UnityShadowLibrary.cginc");
                                string shadowLibraryBakPath = Path.Combine(cgIncludesPath, "UnityShadowLibrary.cginc.bak");
                                if (File.Exists(shadowLibraryPath))
                                    File.Delete(shadowLibraryPath);
                                if (File.Exists(shadowLibraryBakPath))
                                    File.Move(shadowLibraryBakPath, shadowLibraryPath);
                            }

                            installLog = "Unity Local libraries successfully restored. NGSS Local libraries have been deleted.\nPlease restart the Editor to apply changes.";
                            UnityEngine.Debug.Log(installLog);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.Log(ex.Message);
                        installLog = ex.Message;
                        installLocalLibrary = !installLocalLibrary;
                        localLibrary = installLocalLibrary;
                    }
                    finally
                    {
                        if (Directory.Exists(Application.dataPath + "/../Library/ShaderCache"))
                            Directory.Delete(Application.dataPath + "/../Library/ShaderCache", true);
                    }
                }

                if (directionalLibrary != installDirectionalLibrary)
                {
                    try
                    {
                        displayRestart = true;
                        directionalLibrary = installDirectionalLibrary;
                        if (installDirectionalLibrary) //install it
                        {
                            if (GUI.changed && string.IsNullOrEmpty(NGSSAssetPath) == false && (NGSSAssetPath[NGSSAssetPath.Length - 1] == '/' || NGSSAssetPath[NGSSAssetPath.Length - 1].ToString() == @"\"))
                            {
                                NGSSAssetPath = NGSSAssetPath.Remove(NGSSAssetPath.Length - 1);
                            }

                            if (Application.platform == RuntimePlatform.WindowsEditor)
                            {
                                installer_filename = Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe";
                                var proc = Process.Start(installer_filename, "INSTALL_DIRECTIONAL " + "\"" + Application.dataPath + NGSSAssetPath + "\"" + " " + "\"" + cgIncludesPath + "\"");
                            }
                            else
                            {
                                if (File.Exists(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/AutoLight.cginc"))
                                {
                                    //File.Replace(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc.bak");
                                    File.Move(cgIncludesPath + "/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc.bak"); //rename
                                    File.Copy(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/AutoLight.cginc", cgIncludesPath + "/AutoLight.cginc", true); //copy
                                }
                                else
                                {
                                    //GUI.Label(new Rect(10, 15, 330, 75), Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/AutoLight.cginc does not exist make sure the file exist!");
                                    installLog = Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/AutoLight.cginc does not exist make sure the file exist!";
                                    UnityEngine.Debug.LogWarning(installLog);
                                }

                                if (File.Exists(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityDeferredLibrary.cginc"))
                                {
                                    //File.Replace(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc.bak");
                                    File.Move(cgIncludesPath + "/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc.bak"); //rename
                                    File.Copy(Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityDeferredLibrary.cginc", cgIncludesPath + "/UnityDeferredLibrary.cginc", true); //copy
                                }
                                else
                                {
                                    //GUI.Label(new Rect(10, 15, 330, 75), Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityDeferredLibrary.cginc does not exist make sure the file exist!");
                                    installLog = Application.dataPath + NGSSAssetPath + "/NGSS/Libraries/UnityDeferredLibrary.cginc does not exist make sure the file exist!";
                                    UnityEngine.Debug.LogWarning(installLog);
                                }
                            }

                            installLog = "NGSS Directional libraries files successfully installed. Unity Directional libraries have been backed up. Make sure your scene lights have it's corresponding NGSS components.\nPlease restart the Editor to apply changes.";
                            UnityEngine.Debug.Log(installLog);
                        }
                        else //revert it
                        {
                            if (GUI.changed && string.IsNullOrEmpty(NGSSAssetPath) == false && (NGSSAssetPath[NGSSAssetPath.Length - 1] == '/' || NGSSAssetPath[NGSSAssetPath.Length - 1].ToString() == @"\"))
                            {
                                NGSSAssetPath = NGSSAssetPath.Remove(NGSSAssetPath.Length - 1);
                            }

                            if (Application.platform == RuntimePlatform.WindowsEditor)
                            {
                                installer_filename = Application.dataPath + NGSSAssetPath + "/NGSS/Editor/NGSS_Installer.exe";
                                var proc = Process.Start(installer_filename, "DELETE_DIRECTIONAL " + "\"" + Application.dataPath + NGSSAssetPath + "\"" + " " + "\"" + cgIncludesPath + "\"");
                            }
                            else
                            {
                                //AutoLight
                                string autoLightPath = Path.Combine(cgIncludesPath, "AutoLight.cginc");
                                string autoLighBaktPath = Path.Combine(cgIncludesPath, "AutoLight.cginc.bak");
                                if (File.Exists(autoLightPath))
                                    File.Delete(autoLightPath);
                                if (File.Exists(autoLighBaktPath))
                                    File.Move(autoLighBaktPath, autoLightPath);

                                //UnityDeferredLibrary
                                string deferredLightPath = Path.Combine(cgIncludesPath, "UnityDeferredLibrary.cginc");
                                string deferredLightBakPath = Path.Combine(cgIncludesPath, "UnityDeferredLibrary.cginc.bak");
                                if (File.Exists(deferredLightPath))
                                    File.Delete(deferredLightPath);
                                if (File.Exists(deferredLightBakPath))
                                    File.Move(deferredLightBakPath, deferredLightPath);
                            }

                            installLog = "Unity Directional libraries successfully restored. NGSS Directional libraries have been deleted.\nPlease restart the Editor to apply changes.";
                            UnityEngine.Debug.Log(installLog);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.Log(ex.Message);
                        installLog = ex.Message;
                        installDirectionalLibrary = !installDirectionalLibrary;
                        directionalLibrary = installDirectionalLibrary;
                    }
                    finally
                    {
                        if (Directory.Exists(Application.dataPath + "/../Library/ShaderCache"))
                            Directory.Delete(Application.dataPath + "/../Library/ShaderCache", true);
                    }
                }
            }
            else
            {
                GUI.Label(new Rect(10, 15, 330, 75), "NGSS was unable to find the Unity CGIncludes folder.");
                //UnityEngine.Debug.LogWarning("NGSS was unable to find the Unity CGIncludes folder.");
            }

            GUI.EndGroup();
        }
    }
}
#endif
