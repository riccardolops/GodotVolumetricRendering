@tool
extends EditorPlugin

var import_plugin
var simpleITK_manager
var _buildCallable

func _enter_tree():
    _find_editor_buld_shortcut()
    import_plugin = preload("res://addons/sitkvolumetricimporter/SimpleITKImportPlugin.gd").new()
    simpleITK_manager = load("res://addons/sitkvolumetricimporter/SimpleITKManager.cs").new()
    add_import_plugin(import_plugin)
    add_tool_menu_item("Download and setup SimpleITK", self._download_binaries)
    if !simpleITK_manager.HasDownloadedBinaries():
        OS.alert("SimpleITK is not present. Please select Project > Tools > Download and setup SimpleITK", "SimpleITK")

func _exit_tree():
    remove_import_plugin(import_plugin)
    import_plugin = null
    remove_tool_menu_item("Download and setup SimpleITK")
    
func _download_binaries():
    simpleITK_manager.DownloadBinaries(_buildCallable)
    
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
    
