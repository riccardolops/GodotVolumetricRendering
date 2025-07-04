#if TOOLS
using Godot;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

[Tool]
public partial class SimpleITKImporter : EditorPlugin
{
    private Callable _buildCallable;

    private EditorImportPlugin _importPlugin;

    public override void _EnterTree()
    {
        _FindEditorBuildShortcut();
        AddToolMenuItem("Download and setup SimpleITK", Callable.From(_DownloadBinaries));
        if (!_HasDownloadedBinaries())
        {
            OS.Alert("SimpleITK is not present. Please select Project > Tools > Download and setup SimpleITK", "SimpleITK");
        }
        _importPlugin = (EditorImportPlugin)(GD.Load<GDScript>("res://addons/simpleitkimporter/SimpleITKImportPlugin.gd")).New();
        AddImportPlugin(_importPlugin);
    }

    public override void _ExitTree()
    {
        RemoveToolMenuItem("Download and setup SimpleITK");
        RemoveImportPlugin(_importPlugin);
        _importPlugin = null;
    }

    private static bool _HasDownloadedBinaries()
    {
        string binDir = _GetBinaryDirectoryPath();
        return Directory.Exists(binDir) && Directory.GetFiles(binDir).Length > 0;
    }

    public async void _DownloadBinaries()
    {
        GD.Print("Downloading SimpleITK binaries");
        string extractDirPath = _GetBinaryDirectoryPath();
        if (!Directory.Exists(extractDirPath))
        {
            Directory.CreateDirectory(extractDirPath);
        }
        string zipPath = Path.Combine(Directory.GetParent(extractDirPath).FullName, "SimpleITK.zip");
        if (_HasDownloadedBinaries())
        {
            OS.Alert("SimpleITK has already been downloaded. Redownloading will overwrite the existing binaries.", "SimpleITK");
        }
#if GODOT_WINDOWS
        const string downloadURL = "https://github.com/SimpleITK/SimpleITK/releases/download/v2.3.1/SimpleITK-2.3.1-CSharp-win64-x64.zip";
#elif GODOT_LINUXBSD || GODOT_X11
        const string downloadURL = "https://github.com/SimpleITK/SimpleITK/releases/download/v2.3.1/SimpleITK-2.3.1-CSharp-linux.zip";
#elif GODOT_MACOS || GODOT_OSX
        const string downloadURL = "https://github.com/SimpleITK/SimpleITK/releases/download/v2.3.1/SimpleITK-2.3.1-CSharp-macosx-10.9-anycpu.zip";
#endif
        await Task.Run(() => DownloadFileAsync(downloadURL, zipPath));
        if (!File.Exists(zipPath))
        {
            GD.PrintErr("Failed to download SimpleITK binaries.");
            return;
        }
        GD.Print("Extracting SimpleITK binaries");
        try
        {
            ExtractZip(zipPath, extractDirPath);
            string nativeLib = GetBinaryNativeLib(extractDirPath);
            string fromPath = Path.Combine(extractDirPath, nativeLib);
            string toPath = Path.Combine(ProjectSettings.GlobalizePath("res://"), nativeLib);
            if (File.Exists(toPath))
                File.Delete(toPath);
            File.Copy(fromPath, toPath);
        }
        catch (Exception e)
        {
            string errorString = $"Failed to download SimpleITK binaries. Error: {e.Message}"
            + $"Please try downloading the binaries manually from {downloadURL} and extracting it to {extractDirPath}.";
            GD.PrintErr(errorString);
        }
        File.Delete(zipPath);
        GD.Print("Building C# project");
        _buildCallable.Call();
        GD.Print("Finish!");
#if GODOT_LINUXBSD || GODOT_X11
        GD.Print("If \"/usr/share/dotnet/shared/Microsoft.NETCore.App/8.0.0/libSimpleITKCSharpNative.so: cannot open shared object file: No such file or directory\" occurs, try running \"sudo ln /locationtonativelib/libSimpleITKCSharpNative.so /usr/share/dotnet/shared/Microsoft.NETCore.App/8.0.0/libSimpleITKCSharpNative.so");
#endif
    }

    private static string GetBinaryNativeLib(string folderPath)
    {
        string[] files = Directory.GetFiles(folderPath)
            .Where(file => Path.GetFileName(file).Contains("Native"))
            .ToArray();

        if (files.Length > 0)
        {
            return Path.GetFileName(files[0]);
        }
        else
        {
            throw new FileNotFoundException("No files found with 'Native' in their names.");
        }
    }

    public static async Task DownloadFileAsync(string downloadUrl, string savePath)
    {
        using (System.Net.Http.HttpClient client = new())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode(); // Ensure the request was successful

                using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                fileStream = new FileStream(savePath, FileMode.Create, System.IO.FileAccess.Write, FileShare.None, 8192, true))
                {
                    await contentStream.CopyToAsync(fileStream); // Save the content to the file
                }

                GD.Print("File downloaded successfully");
            }
            catch (HttpRequestException e)
            {
                GD.PrintErr($"Error downloading the file: {e.Message}");
            }
        }
    }

    private static void ExtractZip(string zipPath, string extractDirPath)
    {
        // Extract zip
        using FileStream zipStream = new(zipPath, FileMode.Open);
        using ZipArchive archive = new(zipStream, ZipArchiveMode.Update);
        if (!Directory.Exists(extractDirPath))
        {
            _ = Directory.CreateDirectory(extractDirPath);
        }

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.Name != "" && !entry.Name.EndsWith("/"))
            {
                string destFilePath = Path.Combine(extractDirPath, entry.Name);
                //TextAsset destAsset = new TextAsset("abc");
                //AssetDatabase.CreateAsset(destAsset, extractDirRelPath + "/" + entry.Name);
                Stream inStream = entry.Open();

                using Stream outStream = File.OpenWrite(destFilePath);
                inStream.CopyTo(outStream);
            }
        }
    }

    private static string _GetBinaryDirectoryPath()
    {
        var dataPath = ProjectSettings.GlobalizePath("res://");
        return Path.Combine(dataPath, "lib", "SimpleITK");
    }

    private void _FindEditorBuildShortcut()
    {
        // See https://github.com/lewiji/RebuildCsOnFocus/blob/main/addons/rebuild_cs_on_focus/RebuildCsPlugin.cs
        var node = new Control();
        AddControlToBottomPanel(node, "");
        var bottomBar = node.GetParent();
        RemoveControlFromBottomPanel(node);
        node.QueueFree();
        var msBuildPanel = bottomBar.GetChildren()
           .FirstOrDefault(c => c is MarginContainer && c.HasMethod("RebuildProject"));
        if (msBuildPanel != null)
        {
            _buildCallable = new Callable(msBuildPanel, "RebuildProject");
        }
    }
}
#endif
