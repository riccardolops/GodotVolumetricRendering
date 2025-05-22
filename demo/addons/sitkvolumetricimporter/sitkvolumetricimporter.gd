@tool
extends EditorPlugin

var import_plugin
var _buildCallable
signal download_complete

func _enter_tree():
    _find_editor_buld_shortcut()
    download_complete.connect(_buildCallable)
    import_plugin = preload("res://addons/sitkvolumetricimporter/SimpleITKImportPlugin.gd").new()
    add_import_plugin(import_plugin)
    add_tool_menu_item("Download and setup SimpleITK", self._download_binaries)
    if !_has_downloaded_binaries():
        OS.alert("SimpleITK is not present. Please select Project > Tools > Download and setup SimpleITK", "SimpleITK")

func _has_downloaded_binaries() -> bool:
    var bin_dir := _get_binary_directory_path()
    if not DirAccess.dir_exists_absolute(bin_dir):
        return false

    var dir := DirAccess.open(bin_dir)
    if dir == null:
        return false

    dir.list_dir_begin()
    var file = dir.get_next()
    while file != "":
        if not dir.current_is_dir():
            return true  # At least one file found
        file = dir.get_next()
    return false

func _exit_tree():
    remove_import_plugin(import_plugin)
    import_plugin = null
    remove_tool_menu_item("Download and setup SimpleITK")
    
func _download_binaries():
    var extract_dir_path = _get_binary_directory_path()
    if _has_downloaded_binaries():
        OS.alert("SimpleITK has already been downloaded. Re-downloading will overwrite the existing binaries.", "SimpleITK")
        print("SimpleITK has already been downloaded. Re-downloading will overwrite the existing binaries.") 
    else:
        var err = DirAccess.make_dir_recursive_absolute(extract_dir_path)
        if err == OK:
            print("Created directory:", extract_dir_path)
        elif err == ERR_ALREADY_EXISTS:
            print("Directory already exists.")
        else:
            print("Failed to create directory. Error code:", err)
    
        
    # Determine URL based on platform
    var download_url := ""
    if OS.has_feature("windows"):
        download_url = "https://github.com/SimpleITK/SimpleITK/releases/download/v2.3.1/SimpleITK-2.3.1-CSharp-win64-x64.zip"
    elif OS.has_feature("linux") or OS.has_feature("x11"):
        download_url = "https://github.com/SimpleITK/SimpleITK/releases/download/v2.3.1/SimpleITK-2.3.1-CSharp-linux.zip"
    elif OS.has_feature("macos") or OS.has_feature("osx"):
        download_url = "https://github.com/SimpleITK/SimpleITK/releases/download/v2.3.1/SimpleITK-2.3.1-CSharp-macosx-10.9-anycpu.zip"

    var zip_path := extract_dir_path.get_base_dir().path_join("SimpleITK.zip")
    _download_file(download_url,extract_dir_path)

func _download_file(download_url: String, save_path: String) -> void:
    var http_request := HTTPRequest.new()
    add_child(http_request)

    http_request.request_completed.connect(_on_download_completed.bind(save_path))
    print("Downloading SimpleITK binaries...")
    var err := http_request.request(download_url)
    if err != OK:
        push_error("Failed to start HTTP request: %s" % err)

func _on_download_completed(result: int, response_code: int, headers: PackedStringArray, body: PackedByteArray, save_path: String) -> void:
    if result != HTTPRequest.RESULT_SUCCESS or response_code != 200:
        push_error("Download failed. HTTP response code: %d" % response_code)
        return
    print("Unzipping...")
    var compressed_file := FileAccess.create_temp(FileAccess.WRITE_READ, "SimpleITK", "zip")
    if compressed_file:
        compressed_file.store_buffer(body)
        compressed_file.flush()
        compressed_file.close()
    else:
        push_error("Failed to write file to: " + save_path)
        return
    
    var extract_dir := save_path.get_base_dir()
    var zip := ZIPReader.new()
    var err := zip.open(compressed_file.get_path_absolute())
    if err != OK:
        push_error("Failed to open ZIP file.")
        return

    for file_path in zip.get_files():
        var dest_path := extract_dir.path_join("SimpleITK/" + file_path.split("/")[1])
        if file_path.ends_with("/"):
            DirAccess.make_dir_recursive_absolute(dest_path)
        else:
            var content = zip.read_file(file_path)
            var out_file := FileAccess.open(dest_path, FileAccess.WRITE)
            if out_file:
                out_file.store_buffer(content)
                out_file.close()
    zip.close()
    DirAccess.remove_absolute(save_path)
    #download_complete.emit() # why this is not working? I got no clue
    print("Finished!")
    print("Please rebuild your mono project")

func _get_binary_directory_path() -> String:
    var data_path = ProjectSettings.globalize_path("res://")
    return data_path + "lib" + "/SimpleITK"

func _find_editor_buld_shortcut():
    # See https://github.com/lewiji/RebuildCsOnFocus/blob/main/addons/rebuild_cs_on_focus/RebuildCsPlugin.cs
    var node = Control.new()
    add_control_to_bottom_panel(node, "")
    var bottomBar = node.get_parent()
    remove_control_from_bottom_panel(node)
    node.queue_free()
    var msBuildPanel = bottomBar.get_children().filter(func(c):
        return (c is MarginContainer and c.has_method("RebuildProject"))
        ).front() if bottomBar else null
    if msBuildPanel:
        _buildCallable = Callable(msBuildPanel, "RebuildProject")
