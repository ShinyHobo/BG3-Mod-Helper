﻿/// <summary>
/// The spell flag list.
/// </summary>
namespace bg3_modders_multitool.Enums.ValueLists
{
    public enum SpellFlagList
    {
        None,
        HasVerbalComponent,
        HasSomaticComponent,
        IsJump,
        IsAttack,
        IsMelee,
        HasHighGroundRangeExtension,
        IsConcentration,
        AddFallDamageOnLand,
        ConcentrationIgnoresResting,
        InventorySelection,
        IsSpell,
        UNUSED_A,
        IsEnemySpell,
        CannotTargetCharacter,
        CannotTargetItems,
        CannotTargetTerrain,
        IgnoreVisionBlock,
        Stealth,
        AddWeaponRange,
        IgnoreSilence,
        ImmediateCast,
        RangeIgnoreSourceBounds,
        RangeIgnoreTargetBounds,
        RangeIgnoreVerticalThreshold,
        NoSurprise,
        IsHarmful,
        IsTrap,
        IsDefaultWeaponAction,
        UNUSED_B,
        TargetClosestEqualGroundSurface,
        CannotRotate,
        UNUSED_C,
        CanDualWield,
        IsLinkedSpellContainer,
        Invisible,
        AllowMoveAndCast,
        UNUSED_D,
        Wildshape,
        UNUSED_E,
        UnavailableInDialogs,
        TrajectoryRules,
        PickupEntityAndMove,
        Temporary,
        RangeIgnoreBlindness,
        AbortOnSpellRollFail,
        AbortOnSecondarySpellRollFail,
        CanAreaDamageEvade,
        DontAbortPerforming,
        NoCooldownOnMiss,
        NoAOEDamageOnLand,
        IsSwarmAttack,
        DisplayInItemTooltip,
        HideInItemTooltip,
        DisableBlood,
        IgnorePreviouslyPickedEntities,
        CombatLogSetSingleLineRoll,
        NoCameraMove
    }
}