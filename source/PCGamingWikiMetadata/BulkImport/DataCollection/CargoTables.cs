using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;

namespace PCGamingWikiBulkImport.DataCollection;

internal class CargoTables
{
    public static string GameInfoBoxTableName = "Infobox_game";

    public List<CargoFieldInfo> Fields { get; } = [];

    public CargoTables()
    {
        AddListField(GameInfoBoxTableName, "Art_styles", PropertyImportTarget.Tags);
        AddListField(GameInfoBoxTableName, "Controls");
        AddListField(GameInfoBoxTableName, "Genres", PropertyImportTarget.Genres, valueWorkaround: GetGenreWorkaround);
        AddListField(GameInfoBoxTableName, "Microtransactions");
        AddListField(GameInfoBoxTableName, "Modes");
        AddListField(GameInfoBoxTableName, "Monetization");
        AddListField(GameInfoBoxTableName, "Pacing", PropertyImportTarget.Tags);
        AddListField(GameInfoBoxTableName, "Perspectives", PropertyImportTarget.Genres);
        AddListField(GameInfoBoxTableName, "Sports", PropertyImportTarget.Genres);
        AddListField(GameInfoBoxTableName, "Themes", PropertyImportTarget.Tags);
        AddListField(GameInfoBoxTableName, "Vehicles", PropertyImportTarget.Tags);
        AddListField(GameInfoBoxTableName, "Engines", PropertyImportTarget.Tags, pageNamePrefix: "Engine:");
        AddListField(GameInfoBoxTableName, "Series", PropertyImportTarget.Series);
        AddCompanyField("Developers");
        AddCompanyField("Publishers", PropertyImportTarget.Publishers);
        AddCompanyField("Porters_PC_booter");
        AddCompanyField("Porters_DOS");
        AddCompanyField("Porters_Windows_3x");
        AddCompanyField("Porters_Windows");
        AddCompanyField("Porters_Mac_OS");
        AddCompanyField("Porters_OS_X");
        AddCompanyField("Porters_Linux");
        //AddListField(GameInfoBoxTableName, "Available_on", GamePropertyImportTargetField.Platforms);
        AddListField(GameInfoBoxTableName, "Wrappers");
        AddListField(GameInfoBoxTableName, "Wrappers_Windows_3x");
        AddListField(GameInfoBoxTableName, "Wrappers_Windows");
        AddListField(GameInfoBoxTableName, "Wrappers_OS_X");
        AddListField(GameInfoBoxTableName, "Wrappers_Linux");
        AddStringField("Multiplayer", "Local");
        AddStringField("Multiplayer", "Local_players");
        AddListField("Multiplayer", "Local_modes");
        AddStringField("Multiplayer", "LAN");
        AddStringField("Multiplayer", "LAN_players");
        AddListField("Multiplayer", "LAN_modes");
        AddStringField("Multiplayer", "Online");
        AddStringField("Multiplayer", "Online_players");
        AddListField("Multiplayer", "Online_modes");
        AddStringField("Multiplayer", "Asynchronous");
        AddStringField("Multiplayer", "Crossplay");
        AddListField("Multiplayer", "Crossplay_platforms");
        AddStringField("Audio", "Separate_volume_controls");
        AddStringField("Audio", "Surround_sound");
        AddStringField("Audio", "Subtitles");
        AddStringField("Audio", "Closed_captions");
        AddStringField("Audio", "Mute_on_focus_lost");
        AddStringField("Audio", "EAX_support");
        AddStringField("Audio", "Royalty_free_audio");
        AddStringField("Audio", "Red_Book_CD_audio");
        AddStringField("Audio", "General_MIDI_audio");
        AddStringField("Input", "Key_remapping");
        AddStringField("Input", "Mouse_acceleration");
        AddStringField("Input", "Mouse_sensitivity");
        AddStringField("Input", "Mouse_input_in_menus");
        AddStringField("Input", "Keyboard_and_mouse_prompts");
        AddStringField("Input", "Mouse_Y_axis_inversion");
        AddStringField("Input", "Touchscreen");
        AddStringField("Input", "Controller_support");
        AddStringField("Input", "Full_controller_support");
        AddStringField("Input", "Controller_support_level");
        AddStringField("Input", "Controller_remapping");
        AddStringField("Input", "Controller_sensitivity");
        AddStringField("Input", "Controller_Y_axis_inversion");
        AddStringField("Input", "XInput_controller_support");
        AddStringField("Input", "Xbox_prompts");
        AddStringField("Input", "Xbox_One_Impulse_Triggers");
        AddStringField("Input", "Playstation_controller_support");
        AddStringField("Input", "Playstation_prompts");
        AddStringField("Input", "Playstation_motion_sensors");
        AddListField("Input", "Playstation_motion_sensors_modes");
        AddStringField("Input", "Playstation_light_bar_support");
        AddStringField("Input", "DualSense_adaptive_trigger_support");
        AddStringField("Input", "DualSense_haptic_feedback_support");
        AddListField("Input", "PlayStation_controller_models");
        AddListField("Input", "Playstation_connection_modes");
        AddStringField("Input", "Tracked_motion_controllers");
        AddStringField("Input", "Tracked_motion_controller_prompts");
        AddStringField("Input", "Other_controller_support");
        AddListField("Input", "Other_button_prompts");
        AddStringField("Input", "Controller_hotplugging");
        AddStringField("Input", "Input_prompt_override");
        AddStringField("Input", "Controller_haptic_feedback");
        AddStringField("Input", "Simultaneous_input");
        AddStringField("Input", "Steam_Input_API_support");
        AddStringField("Input", "Steam_hook_input");
        AddStringField("Input", "Steam_Input_prompts");
        AddListField("Input", "Steam_Input_prompts_icons");
        AddListField("Input", "Steam_Input_prompts_styles");
        AddStringField("Input", "Steam_Controller_prompts");
        AddStringField("Input", "Steam_Deck_prompts");
        AddStringField("Input", "Steam_Input_motion_sensors");
        AddListField("Input", "Steam_Input_motion_sensors_modes");
        AddStringField("Input", "Steam_Input_presets");
        AddStringField("Input", "Steam_Input_mouse_cursor_detection");
        AddStringField("StarForce", "StarForce_compatible");
        AddStringField("Video", "Multimonitor");
        AddStringField("Video", "Ultrawidescreen");
        AddStringField("Video", "4K_Ultra_HD");
        AddStringField("Video", "Windowed");
        AddStringField("Video", "Borderless_fullscreen_windowed");
        AddStringField("Video", "Anisotropic_filtering");
        AddStringField("Video", "Antialiasing");
        AddListField("Video", "Upscaling");
        AddStringField("Video", "Vsync");
        AddStringField("Video", "60_FPS");
        AddStringField("Video", "120_FPS");
        AddStringField("Video", "HDR");
        AddStringField("Video", "Ray_tracing");
        AddStringField("Video", "Color_blind");
        AddStringField("VR_support", "Native_3D");
        AddStringField("VR_support", "Nvidia_3D_Vision");
        AddStringField("VR_support", "vorpX");
        AddListField("VR_support", "vorpX_modes");
        AddStringField("VR_support", "VR_only");
        AddStringField("VR_support", "OpenXR");
        AddStringField("VR_support", "SteamVR");
        AddStringField("VR_support", "OculusVR");
        AddStringField("VR_support", "Windows_Mixed_Reality");
        AddStringField("VR_support", "OSVR");
        AddStringField("VR_support", "Forte_VFX1");
        AddStringField("VR_support", "Keyboard_mouse");
        AddStringField("VR_support", "Body_tracking");
        AddStringField("VR_support", "Hand_tracking");
        AddStringField("VR_support", "Face_tracking");
        AddStringField("VR_support", "Eye_tracking");
        AddStringField("VR_support", "Tobii_Eye_Tracking");
        AddStringField("VR_support", "TrackIR");
        AddStringField("VR_support", "3RD_Space_Gaming_Vest");
        AddStringField("VR_support", "Novint_Falcon");
        AddStringField("VR_support", "Play_area_seated");
        AddStringField("VR_support", "Play_area_standing");
        AddStringField("VR_support", "Play_area_room_scale");
        AddListField("Availability", "Available_from", PropertyImportTarget.Tags);
        AddListField("Availability", "Available_from_historically", PropertyImportTarget.Tags);
        AddListField("Availability", "Uses_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Removed_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Retail_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Retail_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Developer_website_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Developer_website_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Publisher_website_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Publisher_website_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Official_website_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Official_website_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Amazon_US_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Amazon_US_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Amazon_UK_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Amazon_UK_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Battlenet_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Battlenet_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Bethesdanet_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Bethesdanet_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Discord_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Discord_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "EA_app_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "EA_app_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Epic_Games_Store_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Epic_Games_Store_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "GamersGate_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "GamersGate_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Gamesplanet_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Gamesplanet_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "GOGcom_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "GOGcom_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Green_Man_Gaming_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Green_Man_Gaming_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Humble_Store_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Humble_Store_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "itchio_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "itchio_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Mac_App_Store_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Mac_App_Store_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Meta_Store_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Meta_Store_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Microsoft_Store_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Microsoft_Store_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Steam_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Steam_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Twitch_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Twitch_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Ubisoft_Store_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Ubisoft_Store_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Viveport_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Viveport_keys", PropertyImportTarget.Tags);
        AddListField("Availability", "Zoom_Platform_DRM", PropertyImportTarget.Tags);
        AddListField("Availability", "Zoom_Platform_keys", PropertyImportTarget.Tags);
        AddStringField("Availability", "Apple_Arcade", PropertyImportTarget.Tags);
        AddStringField("Availability", "EA_Play", PropertyImportTarget.Tags);
        AddStringField("Availability", "EA_Play_Pro", PropertyImportTarget.Tags);
        AddStringField("Availability", "EA_Play_Steam", PropertyImportTarget.Tags);
        AddStringField("Availability", "EA_Play_Epic", PropertyImportTarget.Tags);
        AddStringField("Availability", "Ubisoft_Plus ", PropertyImportTarget.Tags);
        AddStringField("Availability", "Xbox_Play_Anywhere", PropertyImportTarget.Tags);
        AddStringField("Availability", "Xbox_Game_Pass", PropertyImportTarget.Tags);
        AddStringField("Availability", "GFWL_type", PropertyImportTarget.Tags);
        AddStringField("Availability", "GFWL_ZDPP", PropertyImportTarget.Tags);
        AddStringField("Availability", "GFWL_local_profile", PropertyImportTarget.Tags);
        //TODO: Middleware
        //TODO: XDG
    }

    private void AddStringField(string table, string field, PropertyImportTarget preferredField = PropertyImportTarget.Features)
    {
        Fields.Add(new CargoFieldInfo { Table = table, Field = field, PreferredField = preferredField, FieldType = CargoFieldType.String });
    }

    private void AddListField(string table, string field, PropertyImportTarget preferredField = PropertyImportTarget.Features, string pageNamePrefix = null, Func<string, CargoValueWorkaround> valueWorkaround = null)
    {
        var f = new CargoFieldInfo
        {
            Table = table,
            Field = field,
            PreferredField = preferredField,
            FieldType = CargoFieldType.ListOfString,
            PageNamePrefix = pageNamePrefix,
        };
        if (valueWorkaround != null)
            f.ValueWorkaround = valueWorkaround;

        Fields.Add(f);
    }

    private void AddCompanyField(string field, PropertyImportTarget preferredField = PropertyImportTarget.Developers, Func<string, CargoValueWorkaround> valueWorkaround = null)
    {
        AddListField(GameInfoBoxTableName, field, preferredField, "Company:", valueWorkaround);
    }

    private static CargoValueWorkaround GetGenreWorkaround(string value)
    {
        if (value == "Sports")
            return new CargoValueWorkaround { Value = "Spo_ts", UseLike = true };

        return new CargoValueWorkaround { Value = value, UseLike = false };
    }
}
