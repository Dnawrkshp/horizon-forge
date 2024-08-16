using System.Collections.Generic;

public enum DLMapIds
{
    SP_Battledome = 1,
    SP_Catacrom = 2,
    SP_Sarathos = 4,
    SP_Kronos = 5,
    SP_Shaar = 6,
    SP_Valix = 7,
    SP_Orxon = 8,
    SP_Torval = 10,
    SP_Stygia = 11,
    SP_Maraxus = 13,
    SP_GhostStation = 14,
    SP_ControlLevel = 15,
    MP_Battledome = 41,
    MP_Catacrom = 42,
    MP_Sarathos = 44,
    MP_DarkCathedral = 45,
    MP_Shaar = 46,
    MP_Valix = 47,
    MP_MiningFacility = 48,
    MP_Torval = 50,
    MP_Tempus = 51,
    MP_Maraxus = 53,
    MP_GhostStation = 54
}

public enum UYAMapIds
{
    SP_Veldin = 1,
    SP_Florana = 2,
    SP_Starship_Pheonix = 3,
    SP_Marcadia = 4,
    SP_Daxx = 5,
    SP_Starship_Pheonix_Under_Attack = 6,
    SP_Annihilation_Nation = 7,
    SP_Aquatos = 8,
    SP_Tyhrranosis = 9,
    SP_Zeldrin_Starport = 10,
    SP_Obani_Moons = 11,
    SP_Rilgar = 12,
    SP_Holostar_Studios_Ratchet = 13,
    SP_Koros = 14,
    SP_Kerwan = 16,
    SP_Crash_Site = 17,
    SP_Aridia = 18,
    SP_Thran_Asteroid_Belt = 19,
    SP_Final_Boss = 20,
    SP_Obani_Draco = 21,
    SP_Mylon = 22,
    SP_Holostar_Studios_Clank = 23,
    SP_Insomniac_Museum = 24,
    SP_Kerwan_Ranger_Missions = 26,
    SP_Aquatos_Base = 27,
    SP_Aquatos_Sewers = 28,
    SP_Tyhrranosis_Ranger_missions = 29,
    MP_Bakisi_Isles = 40,
    MP_Hoven_Gorge = 41,
    MP_Outpost_X12 = 42,
    MP_Korgon_Outpost = 43,
    MP_Metropolis = 44,
    MP_Blackwater_City = 45,
    MP_Command_Center = 46,
    MP_Blackwater_Docks = 47,
    MP_Aquatos_Sewers = 48,
    MP_Marcadia_Palace = 49,
}

public enum GCMapIds
{
    SP_Aranos_1 = 0,
    SP_Oozla = 1,
    SP_Maktar_Nebula = 2,
    SP_Endako = 3,
    SP_Barlow = 4,
    SP_Feltzin_System = 5,
    SP_Notak = 6,
    SP_Siberius = 7,
    SP_Tabora = 8,
    SP_Dobbo = 9,
    SP_Hrugis_Cloud = 10,
    SP_Joba = 11,
    SP_Todano = 12,
    SP_Boldan = 13,
    SP_Aranos_2 = 14,
    SP_Gorn = 15,
    SP_Snivelak = 16,
    SP_Smolg = 17,
    SP_Damosel = 18,
    SP_Grelbin = 19,
    SP_Yeedil = 20,
    SP_Insomniac_Museum = 21,
    SP_Dobbo_Orbit = 22,
    SP_Damosel_Orbit = 23,
    SP_Ship_Shack = 24,
    SP_Wupash_Nebula = 25,
    SP_Jamming_Array = 26
}

public enum DLCustomModeIds
{
    Benchmark = -5,
    InfiniteClimber = -4,
    Spleef = -2,
    None = 0,
    ThousandKillDeathmatch,
    GunGame,
    Infected,
    Payload,
    SearchAndDestroy,
    Survival,
    TeamDefender,
    Training,
    HideAndSeek,
    DreadBall
}

public enum DLTeamIds
{
    Blue,
    Red,
    Green,
    Orange,
    Yellow,
    Purple,
    Pink,
    Aqua,
    Olive,
    Maroon
}

public enum DLFXTextureIds
{
    FX_TEXTURE_FIRST_SPECIAL = -8,
    FX_BACK_ALPHA_CLUT = -8,
    FX_RAW_FRONT_BUFFER = -7,
    FX_RAW_BACK_BUFFER = -6,
    FX_RAW_Z_BUFFER = -5,
    FX_BACK_BUFFER_RECOPY64 = -4,
    FX_BACK_BUFFER_COPY64 = -3,
    FX_BACK_BUFFER_RECOPY = -2,
    FX_BACK_BUFFER_COPY = -1,
    FX_LAME_SHADOW = 0,
    FX_GROUND_OUTER_RETICULE = 1,
    FX_GROUND_INNER_RETICULE = 2,
    FX_CENTER_SCREEN_RETICULE1 = 3,
    FX_CENTER_SCREEN_RETICULE2 = 4,
    FX_GENERIC_RETICULE = 5,
    FX_CMD_ATTACK = 6,
    FX_CMD_DEFEND = 7,
    FX_CMD_EMP = 8,
    FX_CMD_SHIELD = 9,
    FX_CMD_MINE = 10,
    FX_JP_THRUST_GLOW = 11,
    FX_JP_THRUST_HIGHLIGHT = 12,
    FX_JP_THRUST_FIRE = 13,
    FX_LIGHTNING1 = 14,
    FX_ENGINE = 15,
    FX_GLOW_PILL = 16,
    FX_LENS_FLARE_2 = 17,
    FX_SHIP_SHADOW = 18,
    FX_SPARKLE = 19,
    FX_WRENCH_BLUR = 20,
    FX_SUCK_TORNADO = 21,
    FX_WHITE = 22,
    FX_ALPHA_SPARK = 23,
    FX_HOLOGRAM = 24,
    FX_TV_HIGHLIGHT = 25,
    FX_TV_SMALLSCAN = 26,
    FX_HALO = 27,
    FX_TV_SCANLINES = 28,
    FX_TV_SHINE = 29,
    FX_TARGET_RETICULE = 30,
    FX_CONE_FIRE01_SLIM = 31,
    FX_SANDSTORM = 32,
    FX_PROGRESSBAR_INNER = 33,
    FX_PROGRESSBAR_OUTER = 34,
    FX_RYNO_RETICULE = 35,
    FX_SWINGSHOT_RETICULE = 36,
    FX_STATIC = 37,
    FX_BLASTER_RETICULE = 38,
    FX_DEVASTATOR_RETICULE = 39,
    FX_TRIANGLE_RETICULE = 40,
    FX_PLASMA_BALL_CORE = 41,
    FX_PLASMA_BALL_AURA = 42,
    FX_PLASMA_LIGHTNING_BOLT = 43,
    FX_PLASMA_BALL_FLARE = 44,
    FX_PLASMA_BALL_GLOW_RING = 45,
    FX_STEAM_SMOKE_GAS = 46,
    FX_FORK_LIGHTNING = 47,
    FX_FORK_LIGHTNING_GLOW_CORE = 48,
    FX_STARRY_FLASH = 49,
    FX_LAVA_GLOB = 50,
    FX_MAIN_RET1 = 51,
    FX_MAIN_RET2 = 52,
    FX_MAIN_RET3 = 53,
    FX_SMOKE_RING = 54,
    FX_EXPLOTYPE1 = 55,
    FX_SHOCKWAVE = 56,
    FX_EXPLOSION = 57,
    FX_PLASMA_SHOT = 58,
    FX_HEATMASK2 = 59,
    FX_CONCRETE = 60,
    FX_SHOCKWAVE01_KEITH = 61,
    FX_MUZZLEFLASH1 = 62,
    FX_MUZZLEFLASH2 = 63,
    FX_STREAMER_KEITH = 64,
    FX_MUZZLE_FLOWER = 65,
    FX_RADIALBLUR_SNIPER = 66,
    FX_HOLOSHIELD_BASE = 67,
    FX_SNIPER_OUTER_RETICULE = 68,
    FX_REFRACTOR_BEAM = 69,
    FX_SNIPER_INNER_RETICULE = 70,
    FX_STARBURST1_KEITH = 71,
    FX_STARBURST2_KEITH = 72,
    FX_FIRECIRCLE02_KEITH = 73,
    FX_HALFRING_KEITH = 74,
    FX_WHIRLPOOL_KEITH = 75,
    FX_CORONA_KEITH = 76,
    FX_PINCH_ALPHA_MASK = 77,
    FX_DUCK_FEATHER1 = 78,
    FX_DUCK_FEATHER2 = 79,
    FX_CELL_STREAM01 = 80,
    FX_CELL_STREAM02 = 81,
    FX_BULLET_TRAIL_SLIM = 82,
    FX_LIGHTNING02_KEITH = 83,
    FX_LIGHTNING01_SLIM = 84,
    FX_WARPOUT_SHOCKWAVE = 85,
    FX_N60_RETICULE = 86,
    FX_GROUND1_RETICULE = 87,
    FX_GROUND2_RETICULE = 88,
    FX_HEALTH_BALL = 89,
    FX_DISCBLADE_RETICULE = 90,
    FX_SHOCKBLASTER_RETICULE = 91,
    FX_FOCUS_RATCHET_RED = 92,
    FX_FOCUS_RATCHET_BLUE = 93,
    FX_FOCUS_RATCHET_RED_DEAD = 94,
    FX_FOCUS_RATCHET_BLUE_DEAD = 95,
    FX_LOCK_ON_RETICULE = 96,
    FX_CRACKS = 97,
    FX_LEVEL_0 = 98,
    FX_LEVEL_1 = 99,
    FX_LEVEL_2 = 100,
    FX_LEVEL_3 = 101,
    FX_LEVEL_4 = 102,
    FX_LEVEL_5 = 103,
    FX_LEVEL_6 = 104,
    FX_LEVEL_7 = 105,
    FX_LEVEL_8 = 106,
    FX_LEVEL_9 = 107,
    FX_LEVEL_10 = 108,
    FX_LEVEL_11 = 109,
    FX_LEVEL_12 = 110,
    FX_LEVEL_13 = 111,
    FX_LEVEL_14 = 112,
    FX_LEVEL_15 = 113,
    FX_LEVEL_16 = 114,
    FX_LEVEL_17 = 115,
    FX_LEVEL_18 = 116,
    FX_LEVEL_19 = 117,
    FX_LEVEL_20 = 118,
    FX_LEVEL_21 = 119,
    FX_LEVEL_22 = 120,
    FX_LEVEL_23 = 121,
    FX_LEVEL_24 = 122,
    FX_LEVEL_25 = 123
}

public enum DLLevelFXTextureIds
{
    FX_LEVEL_OFF = -1,
    FX_LEVEL_0 = 0,
    FX_LEVEL_1 = 1,
    FX_LEVEL_2 = 2,
    FX_LEVEL_3 = 3,
    FX_LEVEL_4 = 4,
    FX_LEVEL_5 = 5,
    FX_LEVEL_6 = 6,
    FX_LEVEL_7 = 7,
    FX_LEVEL_8 = 8,
    FX_LEVEL_9 = 9,
    FX_LEVEL_10 = 10,
    FX_LEVEL_11 = 11,
    FX_LEVEL_12 = 12,
    FX_LEVEL_13 = 13,
    FX_LEVEL_14 = 14,
    FX_LEVEL_15 = 15,
    FX_LEVEL_16 = 16,
    FX_LEVEL_17 = 17,
    FX_LEVEL_18 = 18,
    FX_LEVEL_19 = 19,
    FX_LEVEL_20 = 20,
    FX_LEVEL_21 = 21,
    FX_LEVEL_22 = 22,
    FX_LEVEL_23 = 23,
    FX_LEVEL_24 = 24,
    FX_LEVEL_25 = 25,
}

public enum GameRegion
{
    NTSC,
    PAL
}

public static class Constants
{
    public static readonly string ForgeVersion = "v1.1.27";

    public static readonly string RepoUrl = "https://github.com/Horizon-Private-Server/horizon-forge";
    public static readonly string WikiUrl = $"{RepoUrl}/wiki";
    public static readonly string DiscordInviteUrl = "https://discord.gg/invite/horizonps";

    public static readonly Dictionary<int, string> GameAssetTag = new Dictionary<int, string>() { { 4, "DL" }, { 3, "UYA" }, { 2, "GC" } };

    public static readonly int MAX_TEXTURE_SIZE = 512;
    public static readonly int MIN_TEXTURE_SIZE = 32;

    public static readonly Dictionary<DLMapIds, int> DLMapIndex = new()
    {
        { DLMapIds.MP_Battledome, 0 },
        { DLMapIds.MP_Catacrom, 1 },
        { DLMapIds.MP_Sarathos, 2 },
        { DLMapIds.MP_DarkCathedral, 3 },
        { DLMapIds.MP_Shaar, 4 },
        { DLMapIds.MP_Valix, 5 },
        { DLMapIds.MP_MiningFacility, 6 },
        { DLMapIds.MP_Torval, 7 },
        { DLMapIds.MP_Tempus, 8 },
        { DLMapIds.MP_Maraxus, 9 },
        { DLMapIds.MP_GhostStation, 10 },
    };

}

public static class RCVER
{
    public const int DL = 4;
    public const int UYA = 3;
    public const int GC = 2;
}
