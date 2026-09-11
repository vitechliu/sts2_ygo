extends SceneTree

# 从实际导出的 PCK 读取清单和语言文件，不用导出日志推断资源存在。
func _initialize():
    var args = OS.get_cmdline_user_args()
    if args.size() != 1 or not ProjectSettings.load_resource_pack(args[0]):
        push_error("无法加载待检查的 PCK")
        quit(1)
        return
    var manifest = JSON.parse_string(FileAccess.get_file_as_string("res://VYgo/ui_skin/skin.json"))
    if not manifest is Dictionary or manifest.get("schemaVersion") != 1:
        push_error("PCK 内缺少有效的皮肤清单")
        quit(1)
        return
    for rule in manifest.get("rules", []):
        for texture in rule.states.values():
            if not ResourceLoader.exists(texture) or load(texture) == null:
                push_error("皮肤纹理缺失：" + texture)
                quit(1)
                return
    for lang in ["zhs", "eng", "jpn"]:
        var data = JSON.parse_string(FileAccess.get_file_as_string("res://VYgo/localization/" + lang + "/settings_ui.json"))
        if not data is Dictionary or not data.has("VYGO_SETTINGS_REPLACE_UI_SKIN") or not data.has("VYGO_SETTINGS_REPLACE_UI_SKIN_DESCRIPTION"):
            push_error("设置文案缺失：" + lang)
            quit(1)
            return
    print("UI_SKIN_PCK_CHECK_PASSED rules=", manifest.get("rules", []).size(), " languages=3")
    quit(0)
