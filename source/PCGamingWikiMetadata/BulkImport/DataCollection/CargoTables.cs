using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;

namespace PCGamingWikiBulkImport.DataCollection;

internal class CargoTables
{
    public static class Names
    {
        public const string GameInfoBox = "Infobox_game";
        public const string Multiplayer = "Multiplayer";
        public const string Audio = "Audio";
        public const string Input = "Input";
        public const string Video = "Video";
        public const string VrSupport = "VR_support";
        public const string Availability = "Availability";
        public const string Middleware = "Middleware";
    }

    public List<CargoFieldInfo> Fields { get; } = [];

    public CargoTables()
    {
        AddListField(Names.GameInfoBox, "Art_styles", PropertyImportTarget.Tags);
        AddListField(Names.GameInfoBox, "Controls");
        AddListField(Names.GameInfoBox, "Genres", PropertyImportTarget.Genres, valueWorkaround: GetGenreWorkaround);
        AddListField(Names.GameInfoBox, "Microtransactions");
        AddListField(Names.GameInfoBox, "Modes");
        AddListField(Names.GameInfoBox, "Monetization");
        AddListField(Names.GameInfoBox, "Pacing", PropertyImportTarget.Tags);
        AddListField(Names.GameInfoBox, "Perspectives", PropertyImportTarget.Genres);
        AddListField(Names.GameInfoBox, "Sports", PropertyImportTarget.Genres);
        AddListField(Names.GameInfoBox, "Themes", PropertyImportTarget.Tags);
        AddListField(Names.GameInfoBox, "Vehicles", PropertyImportTarget.Tags);
        AddListField(Names.GameInfoBox, "Engines", PropertyImportTarget.Tags, pageNamePrefix: "Engine:");
        AddListField(Names.GameInfoBox, "Series", PropertyImportTarget.Series);
        AddCompanyField("Developers");
        AddCompanyField("Publishers", PropertyImportTarget.Publishers);
        AddCompanyField("Porters_PC_booter");
        AddCompanyField("Porters_DOS");
        AddCompanyField("Porters_Windows_3x");
        AddCompanyField("Porters_Windows");
        AddCompanyField("Porters_Mac_OS");
        AddCompanyField("Porters_OS_X");
        AddCompanyField("Porters_Linux");
        //AddListField(Names.GameInfoBox, "Available_on", GamePropertyImportTargetField.Platforms);
        AddListField(Names.GameInfoBox, "Wrappers");
        AddListField(Names.GameInfoBox, "Wrappers_Windows_3x");
        AddListField(Names.GameInfoBox, "Wrappers_Windows");
        AddListField(Names.GameInfoBox, "Wrappers_OS_X");
        AddListField(Names.GameInfoBox, "Wrappers_Linux");
        AddStringField(Names.Multiplayer, "Local");
        AddStringField(Names.Multiplayer, "Local_players");
        AddListField(Names.Multiplayer, "Local_modes");
        AddStringField(Names.Multiplayer, "LAN");
        AddStringField(Names.Multiplayer, "LAN_players");
        AddListField(Names.Multiplayer, "LAN_modes");
        AddStringField(Names.Multiplayer, "Online");
        AddStringField(Names.Multiplayer, "Online_players");
        AddListField(Names.Multiplayer, "Online_modes");
        AddStringField(Names.Multiplayer, "Asynchronous");
        AddStringField(Names.Multiplayer, "Crossplay");
        AddListField(Names.Multiplayer, "Crossplay_platforms");
        AddStringField(Names.Audio, "Separate_volume_controls");
        AddStringField(Names.Audio, "Surround_sound");
        AddStringField(Names.Audio, "Subtitles");
        AddStringField(Names.Audio, "Closed_captions");
        AddStringField(Names.Audio, "Mute_on_focus_lost");
        AddStringField(Names.Audio, "EAX_support");
        AddStringField(Names.Audio, "Royalty_free_audio");
        AddStringField(Names.Audio, "Red_Book_CD_audio");
        AddStringField(Names.Audio, "General_MIDI_audio");
        AddStringField(Names.Input, "Key_remapping");
        AddStringField(Names.Input, "Mouse_acceleration");
        AddStringField(Names.Input, "Mouse_sensitivity");
        AddStringField(Names.Input, "Mouse_input_in_menus");
        AddStringField(Names.Input, "Keyboard_and_mouse_prompts");
        AddStringField(Names.Input, "Mouse_Y_axis_inversion");
        AddStringField(Names.Input, "Touchscreen");
        AddStringField(Names.Input, "Controller_support");
        AddStringField(Names.Input, "Full_controller_support");
        AddStringField(Names.Input, "Controller_support_level");
        AddStringField(Names.Input, "Controller_remapping");
        AddStringField(Names.Input, "Controller_sensitivity");
        AddStringField(Names.Input, "Controller_Y_axis_inversion");
        AddStringField(Names.Input, "XInput_controller_support");
        AddStringField(Names.Input, "Xbox_prompts");
        AddStringField(Names.Input, "Xbox_One_Impulse_Triggers");
        AddStringField(Names.Input, "Playstation_controller_support");
        AddStringField(Names.Input, "Playstation_prompts");
        AddStringField(Names.Input, "Playstation_motion_sensors");
        AddListField(Names.Input, "Playstation_motion_sensors_modes");
        AddStringField(Names.Input, "Playstation_light_bar_support");
        AddStringField(Names.Input, "DualSense_adaptive_trigger_support");
        AddStringField(Names.Input, "DualSense_haptic_feedback_support");
        AddListField(Names.Input, "PlayStation_controller_models");
        AddListField(Names.Input, "Playstation_connection_modes");
        AddStringField(Names.Input, "Tracked_motion_controllers");
        AddStringField(Names.Input, "Tracked_motion_controller_prompts");
        AddStringField(Names.Input, "Other_controller_support");
        AddListField(Names.Input, "Other_button_prompts");
        AddStringField(Names.Input, "Controller_hotplugging");
        AddStringField(Names.Input, "Input_prompt_override");
        AddStringField(Names.Input, "Controller_haptic_feedback");
        AddStringField(Names.Input, "Simultaneous_input");
        AddStringField(Names.Input, "Steam_Input_API_support");
        AddStringField(Names.Input, "Steam_hook_input");
        AddStringField(Names.Input, "Steam_Input_prompts");
        AddListField(Names.Input, "Steam_Input_prompts_icons");
        AddListField(Names.Input, "Steam_Input_prompts_styles");
        AddStringField(Names.Input, "Steam_Controller_prompts");
        AddStringField(Names.Input, "Steam_Deck_prompts");
        AddStringField(Names.Input, "Steam_Input_motion_sensors");
        AddListField(Names.Input, "Steam_Input_motion_sensors_modes");
        AddStringField(Names.Input, "Steam_Input_presets");
        AddStringField(Names.Input, "Steam_Input_mouse_cursor_detection");
        AddStringField("StarForce", "StarForce_compatible");
        AddStringField(Names.Video, "Multimonitor");
        AddStringField(Names.Video, "Ultrawidescreen");
        AddStringField(Names.Video, "4K_Ultra_HD");
        AddStringField(Names.Video, "Windowed");
        AddStringField(Names.Video, "Borderless_fullscreen_windowed");
        AddStringField(Names.Video, "Anisotropic_filtering");
        AddStringField(Names.Video, "Antialiasing");
        AddListField(Names.Video, "Upscaling");
        AddStringField(Names.Video, "Vsync");
        AddStringField(Names.Video, "60_FPS");
        AddStringField(Names.Video, "120_FPS");
        AddStringField(Names.Video, "HDR");
        AddStringField(Names.Video, "Ray_tracing");
        AddStringField(Names.Video, "Color_blind");
        AddStringField(Names.VrSupport, "Native_3D");
        AddStringField(Names.VrSupport, "Nvidia_3D_Vision");
        AddStringField(Names.VrSupport, "vorpX");
        AddListField(Names.VrSupport, "vorpX_modes");
        AddStringField(Names.VrSupport, "VR_only");
        AddStringField(Names.VrSupport, "OpenXR");
        AddStringField(Names.VrSupport, "SteamVR");
        AddStringField(Names.VrSupport, "OculusVR");
        AddStringField(Names.VrSupport, "Windows_Mixed_Reality");
        AddStringField(Names.VrSupport, "OSVR");
        AddStringField(Names.VrSupport, "Forte_VFX1");
        AddStringField(Names.VrSupport, "Keyboard_mouse");
        AddStringField(Names.VrSupport, "Body_tracking");
        AddStringField(Names.VrSupport, "Hand_tracking");
        AddStringField(Names.VrSupport, "Face_tracking");
        AddStringField(Names.VrSupport, "Eye_tracking");
        AddStringField(Names.VrSupport, "Tobii_Eye_Tracking");
        AddStringField(Names.VrSupport, "TrackIR");
        AddStringField(Names.VrSupport, "3RD_Space_Gaming_Vest");
        AddStringField(Names.VrSupport, "Novint_Falcon");
        AddStringField(Names.VrSupport, "Play_area_seated");
        AddStringField(Names.VrSupport, "Play_area_standing");
        AddStringField(Names.VrSupport, "Play_area_room_scale");
        AddListField(Names.Availability, "Available_from", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Available_from_historically", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Uses_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Removed_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Retail_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Retail_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Developer_website_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Developer_website_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Publisher_website_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Publisher_website_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Official_website_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Official_website_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Amazon_US_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Amazon_US_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Amazon_UK_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Amazon_UK_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Battlenet_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Battlenet_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Bethesdanet_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Bethesdanet_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Discord_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Discord_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "EA_app_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "EA_app_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Epic_Games_Store_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Epic_Games_Store_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "GamersGate_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "GamersGate_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Gamesplanet_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Gamesplanet_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "GOGcom_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "GOGcom_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Green_Man_Gaming_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Green_Man_Gaming_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Humble_Store_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Humble_Store_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "itchio_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "itchio_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Mac_App_Store_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Mac_App_Store_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Meta_Store_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Meta_Store_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Microsoft_Store_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Microsoft_Store_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Steam_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Steam_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Twitch_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Twitch_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Ubisoft_Store_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Ubisoft_Store_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Viveport_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Viveport_keys", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Zoom_Platform_DRM", PropertyImportTarget.Tags);
        AddListField(Names.Availability, "Zoom_Platform_keys", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "Apple_Arcade", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "EA_Play", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "EA_Play_Pro", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "EA_Play_Steam", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "EA_Play_Epic", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "Ubisoft_Plus ", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "Xbox_Play_Anywhere", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "Xbox_Game_Pass", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "GFWL_type", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "GFWL_ZDPP", PropertyImportTarget.Tags);
        AddStringField(Names.Availability, "GFWL_local_profile", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Physics", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Interface", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Input", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Cutscenes", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Multiplayer", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Anticheat", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Removed_Physics", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Removed_Interface", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Removed_Input", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Removed_Cutscenes", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Removed_Multiplayer", PropertyImportTarget.Tags);
        AddListField(Names.Middleware, "Removed_Anticheat", PropertyImportTarget.Tags);
        AddStringField("XDG", "Supported");
        AddStringField("GOGcom_Enhancement_Project", "Press_account", PropertyImportTarget.Tags);
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
        AddListField(Names.GameInfoBox, field, preferredField, "Company:", valueWorkaround);
    }

    private static CargoValueWorkaround GetGenreWorkaround(string value)
    {
        if (value == "Sports")
            return new CargoValueWorkaround { Value = "Spo_ts", UseLike = true };

        return new CargoValueWorkaround { Value = value, UseLike = false };
    }
}
