@tool
extends EditorPlugin

const HOST_BLOCK_BEGIN := "<!-- Ctis:Begin -->"
const HOST_BLOCK_END := "<!-- Ctis:End -->"
const HOST_BLOCK := """<!-- Ctis:Begin -->
  <ItemGroup>
	<Compile Remove="addons/ctis/**/*.cs" />
	<Compile Remove="tests/Ctis.Tests/**/*.cs" />
  </ItemGroup>
  <ItemGroup>
	<Compile Include="addons/ctis/editor/CtisDataEditorHost.cs" />
  </ItemGroup>
  <ItemGroup>
	<ProjectReference Include="addons/ctis/Core/Ctis.Core.csproj" />
	<ProjectReference Include="addons/ctis/Godot/Ctis.Godot.csproj" />
  </ItemGroup>
<!-- Ctis:End -->
"""

const DOTPUDICA_PLUGIN := "res://addons/dot-pudica/plugin.cfg"
const COORD_PLUGIN := "res://addons/tetris_coord_lib/plugin.cfg"


func _enter_tree() -> void:
	_ensure_dependencies()
	_ensure_tests_gdignore()
	_update_host_project(true)
	_ensure_catalog_setting()
	add_tool_menu_item("CTIS/Data Editor", _open_data_editor)
	print("[CTIS] Plugin enabled.")


func _exit_tree() -> void:
	remove_tool_menu_item("CTIS/Data Editor")
	_close_data_editor()
	if not _is_plugin_still_enabled():
		_update_host_project(false)
		print("[CTIS] Plugin disabled; host project block removed if present.")
	else:
		print("[CTIS] Plugin reloading; host project block kept.")


var _editor_window: Window


func _open_data_editor() -> void:
	_close_data_editor()
	_editor_window = _create_editor_window()
	if _editor_window == null:
		return
	var base := EditorInterface.get_base_control()
	base.add_child(_editor_window)
	_editor_window.close_requested.connect(_close_data_editor)
	_editor_window.popup_centered(Vector2i(1100, 720))
	print("[CTIS] Data Editor opened.")


func _create_editor_window() -> Window:
	var path := _host_script_path()
	if not ResourceLoader.exists(path):
		push_error("[CTIS] Missing %s." % path)
		return null
	var script := load(path)
	if script == null:
		push_error("[CTIS] Failed to load %s. Rebuild C# so the host project compiles this file." % path)
		return null
	var host = script.new()
	if host is Window:
		return host
	push_error("[CTIS] CtisDataEditorHost did not instantiate as a Window. Rebuild C# assemblies and reload the plugin.")
	if host != null:
		host.free()
	return null


func _host_script_path() -> String:
	var script := get_script() as Script
	return script.resource_path.get_base_dir().path_join("editor/CtisDataEditorHost.cs")


func _close_data_editor() -> void:
	if _editor_window == null:
		return
	if is_instance_valid(_editor_window):
		_editor_window.queue_free()
	_editor_window = null


func _ensure_catalog_setting() -> void:
	_ensure_path_setting("ctis/item_catalog", "", "*.json")
	_ensure_path_setting("ctis/placement_config", "", "*.json")
	_ensure_path_setting("ctis/equipment_layout", "", "*.json")
	_ensure_path_setting("ctis/locale", "", "*.csv")
	_ensure_path_setting("ctis/menu_theme", "res://addons/ctis/Art/Theme/ctis_menu_theme.tres", "*.tres")
	_ensure_path_setting("ctis/scenes/pocket", "res://addons/ctis/Godot/Scenes/GridPanels/GP_Pocket.tscn", "*.tscn")
	_ensure_path_setting("ctis/scenes/coffer", "res://addons/ctis/Godot/Scenes/GridPanels/GP_Coffer.tscn", "*.tscn")


func _ensure_path_setting(setting_name: String, default_path: String, filter: String) -> void:
	if not ProjectSettings.has_setting(setting_name):
		ProjectSettings.set_setting(setting_name, default_path)
	ProjectSettings.set_as_basic(setting_name, true)
	ProjectSettings.add_property_info({
		"name": setting_name,
		"type": TYPE_STRING,
		"hint": PROPERTY_HINT_FILE,
		"hint_string": filter
	})


func _ensure_dependencies() -> void:
	var enabled: PackedStringArray = ProjectSettings.get_setting("editor_plugins/enabled", PackedStringArray())
	var has_pudica := false
	var has_coord := false
	for entry in enabled:
		var path := str(entry).replace("\\", "/")
		if path == DOTPUDICA_PLUGIN:
			has_pudica = true
		elif path == COORD_PLUGIN:
			has_coord = true
	if not has_pudica:
		push_error("[CTIS] Requires DotPudica. Enable res://addons/dot-pudica/plugin.cfg first.")
	if not has_coord:
		push_error("[CTIS] Requires Tetris Coord Lib. Enable res://addons/tetris_coord_lib/plugin.cfg first.")


func _ensure_tests_gdignore() -> void:
	var tests_dir := ProjectSettings.globalize_path("res://tests/Ctis.Tests")
	if not DirAccess.dir_exists_absolute(tests_dir):
		return
	var ignore_path := tests_dir.path_join(".gdignore")
	if FileAccess.file_exists(ignore_path):
		return
	var file := FileAccess.open(ignore_path, FileAccess.WRITE)
	if file == null:
		return
	file.store_string("# Ignore pure .NET unit tests from Godot import / C# UID generation.\n")
	file.close()


func _is_plugin_still_enabled() -> bool:
	var enabled: PackedStringArray = ProjectSettings.get_setting("editor_plugins/enabled", PackedStringArray())
	var script := get_script() as Script
	if script == null:
		return false
	var plugin_path: String = script.resource_path.get_base_dir().path_join("plugin.cfg")
	for entry in enabled:
		if str(entry).replace("\\", "/") == plugin_path.replace("\\", "/"):
			return true
	return false


func _update_host_project(enable: bool) -> void:
	var csproj_path := _find_host_csproj()
	if csproj_path.is_empty():
		print("[CTIS] No host .csproj found under res://, skipping project injection.")
		return
	var content := FileAccess.get_file_as_string(csproj_path)
	if content.is_empty():
		push_error("[CTIS] Failed to read %s" % csproj_path)
		return
	var updated := _sync_host_block(content) if enable else _remove_host_block(content)
	if updated == content:
		return
	var file := FileAccess.open(csproj_path, FileAccess.WRITE)
	if file == null:
		push_error("[CTIS] Failed to open %s for writing" % csproj_path)
		return
	file.store_string(updated)
	file.close()
	print("[CTIS] %s host project configuration: %s" % ["Synced" if enable else "Removed", csproj_path])


func _find_host_csproj() -> String:
	var root := ProjectSettings.globalize_path("res://")
	var dir := DirAccess.open(root)
	if dir == null:
		return ""
	dir.list_dir_begin()
	while true:
		var entry := dir.get_next()
		if entry.is_empty():
			break
		if dir.current_is_dir():
			continue
		if entry.get_extension().to_lower() != "csproj":
			continue
		var path := root.path_join(entry)
		if path.replace("\\", "/").to_lower().contains("/addons/"):
			continue
		dir.list_dir_end()
		return path
	dir.list_dir_end()
	return ""


func _sync_host_block(content: String) -> String:
	var begin_index := content.find(HOST_BLOCK_BEGIN)
	if begin_index == -1:
		return _inject_host_block(content)
	var end_index := content.find(HOST_BLOCK_END, begin_index)
	if end_index == -1:
		push_error("[CTIS] Invalid injected block: missing end marker")
		return content
	end_index += HOST_BLOCK_END.length()
	while end_index < content.length() and (content[end_index] == "\n" or content[end_index] == "\r"):
		end_index += 1
	var existing := content.substr(begin_index, end_index - begin_index)
	if existing.replace("\r\n", "\n").strip_edges() == HOST_BLOCK.replace("\r\n", "\n").strip_edges():
		return content
	var prefix := _trim_right_whitespace(content.substr(0, begin_index))
	var suffix := _trim_left_newlines(content.substr(end_index))
	if suffix.is_empty():
		return "%s\n\n%s\n" % [prefix, HOST_BLOCK]
	return "%s\n\n%s\n%s" % [prefix, HOST_BLOCK, suffix]


func _inject_host_block(content: String) -> String:
	var close_index := content.rfind("</Project>")
	if close_index == -1:
		push_error("[CTIS] Invalid .csproj format: missing </Project>")
		return content
	var prefix := _trim_right_whitespace(content.substr(0, close_index))
	return "%s\n\n%s</Project>\n" % [prefix, HOST_BLOCK]


func _remove_host_block(content: String) -> String:
	var begin_index := content.find(HOST_BLOCK_BEGIN)
	if begin_index == -1:
		return content
	var end_index := content.find(HOST_BLOCK_END, begin_index)
	if end_index == -1:
		return content
	end_index += HOST_BLOCK_END.length()
	if end_index < content.length() and content[end_index] == "\n":
		end_index += 1
	var prefix := _trim_right_whitespace(content.substr(0, begin_index))
	var suffix := _trim_left_newlines(content.substr(end_index))
	if suffix.is_empty():
		return "%s\n" % prefix
	return "%s\n\n%s" % [prefix, suffix]


func _trim_right_whitespace(text: String) -> String:
	var end_index := text.length() - 1
	while end_index >= 0:
		var ch := text[end_index]
		if ch != " " and ch != "\t" and ch != "\r" and ch != "\n":
			break
		end_index -= 1
	return "" if end_index < 0 else text.substr(0, end_index + 1)


func _trim_left_newlines(text: String) -> String:
	var start_index := 0
	while start_index < text.length() and (text[start_index] == "\r" or text[start_index] == "\n"):
		start_index += 1
	return text.substr(start_index)
