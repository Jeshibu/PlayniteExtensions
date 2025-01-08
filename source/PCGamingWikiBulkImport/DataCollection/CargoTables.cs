using PlayniteExtensions.Metadata.Common;
using System.Collections.Generic;

namespace PCGamingWikiBulkImport.DataCollection
{
    internal class CargoTables
    {
        public static string GameInfoBoxTableName = "Infobox_game";

        public List<CargoFieldInfo> Fields { get; } = new List<CargoFieldInfo>();

        public CargoTables()
        {
            AddTaxonomyField("Art_styles", "Art styles", PropertyImportTarget.Tags);
            AddTaxonomyField("Controls", "Controls");
            AddTaxonomyField("Genres", "Genres", PropertyImportTarget.Genres);
            AddTaxonomyField("Microtransactions", "Microtransactions");
            AddTaxonomyField("Modes", "Modes");
            AddTaxonomyField("Monetization", "Monetization");
            AddTaxonomyField("Pacing", "Pacing", PropertyImportTarget.Tags);
            AddTaxonomyField("Perspectives", "Perspectives", PropertyImportTarget.Genres);
            AddTaxonomyField("Sports", "Sports subcategories", PropertyImportTarget.Genres);
            AddTaxonomyField("Themes", "Theme", PropertyImportTarget.Tags);
            AddTaxonomyField("Vehicles", "Vehicle subcategories", PropertyImportTarget.Tags);
            AddReferenceField(GameInfoBoxTableName, "Engines", "Engine", preferredField: PropertyImportTarget.Tags);
            AddReferenceField(GameInfoBoxTableName, "Developers", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Porters_PC_booter", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Porters_DOS", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Porters_Windows_3x", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Porters_Windows", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Porters_Mac_OS", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Porters_OS_X", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Porters_Linux", "Company", preferredField: PropertyImportTarget.Developers);
            AddReferenceField(GameInfoBoxTableName, "Publishers", "Company", preferredField: PropertyImportTarget.Publishers);
            //AddStringField(GameInfoBoxTableName, "Available_on", GamePropertyImportTargetField.Platforms);
            AddStringField(GameInfoBoxTableName, "Wrappers_Windows_3x");
            AddStringField(GameInfoBoxTableName, "Wrappers_Windows");
            AddStringField(GameInfoBoxTableName, "Wrappers_OS_X");
            AddStringField(GameInfoBoxTableName, "Wrappers_Linux");
            //TODO: Infobox_game.Series - should work fine with a count query, but has no reference table
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
            AddStringField("Input", "Playstation_motion_sensors_modes");
            AddStringField("Input", "Playstation_light_bar_support");
            AddStringField("Input", "DualSense_adaptive_trigger_support");
            AddStringField("Input", "DualSense_haptic_feedback_support");
            AddStringField("Input", "PlayStation_controller_models");
            AddStringField("Input", "Playstation_connection_modes");
            AddStringField("Input", "Tracked_motion_controllers");
            AddStringField("Input", "Tracked_motion_controller_prompts");
            AddStringField("Input", "Other_controller_support");
            AddStringField("Input", "Other_button_prompts");
            AddStringField("Input", "Controller_hotplugging");
            AddStringField("Input", "Input_prompt_override");
            AddStringField("Input", "Controller_haptic_feedback");
            AddStringField("Input", "Simultaneous_input");
            AddStringField("Input", "Steam_Input_API_support");
            AddStringField("Input", "Steam_hook_input");
            AddStringField("Input", "Steam_Input_prompts");
            AddStringField("Input", "Steam_Input_prompts_icons");
            AddStringField("Input", "Steam_Input_prompts_styles");
            AddStringField("Input", "Steam_Controller_prompts");
            AddStringField("Input", "Steam_Deck_prompts");
            AddStringField("Input", "Steam_Input_motion_sensors");
            AddStringField("Input", "Steam_Input_motion_sensors_modes");
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
            AddStringField("Video", "Upscaling");
            AddStringField("Video", "Vsync");
            AddStringField("Video", "60_FPS");
            AddStringField("Video", "120_FPS");
            AddStringField("Video", "HDR");
            AddStringField("Video", "Ray_tracing");
            AddStringField("Video", "Color_blind");
            AddStringField("VR_support", "Native_3D");
            AddStringField("VR_support", "Nvidia_3D_Vision");
            AddStringField("VR_support", "vorpX");
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
            //TODO: Middleware
            //TODO: XDG
        }

        private void AddTaxonomyField(string infoboxGameField, string taxonomyCategory, PropertyImportTarget preferredField = PropertyImportTarget.Features)
        {
            AddReferenceField(GameInfoBoxTableName, infoboxGameField, "Taxonomy", "Glossary", $"Taxonomy.Category='{taxonomyCategory}'", preferredField);
        }

        private void AddReferenceField(string table, string field, string refTable, string refField = "_PageName", string refWhere = null, PropertyImportTarget preferredField = PropertyImportTarget.Features)
        {
            Fields.Add(new CargoFieldInfo
            {
                Table = table,
                Field = field,
                PreferredField = preferredField,
                EntityDefinition = new EntityDefinitionInfo { Table = refTable, Field = refField, Where = refWhere }
            });
        }

        private void AddStringField(string table, string field, PropertyImportTarget preferredField = PropertyImportTarget.Features)
        {
            Fields.Add(new CargoFieldInfo { Table = table, Field = field, PreferredField = preferredField });
        }
    }
}
