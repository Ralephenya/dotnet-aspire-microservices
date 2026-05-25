namespace GACS.Components.Themes;

/// <summary>
/// GACS design token names for use in CSS custom properties and Fluent theme overrides.
/// Reference: FRONTEND-ARCHITECTURE.md Section 4.2
/// </summary>
public static class GacsTokens
{
    // Brand tokens
    public const string ColorBrandPrimary = "--colorBrandPrimary";
    public const string ColorBrandSecondary = "--colorBrandSecondary";
    public const string ColorNeutralBackground = "--colorNeutralBackground";
    public const string ColorNeutralSurface = "--colorNeutralSurface";
    public const string ColorNeutralBorder = "--colorNeutralBorder";
    public const string ColorNeutralForeground = "--colorNeutralForeground";
    public const string ColorNeutralForegroundSubtle = "--colorNeutralForegroundSubtle";

    // Risk tokens — light theme values
    public const string ColorRiskGreen = "--colorRiskGreen";            // #107C10
    public const string ColorRiskGreenBackground = "--colorRiskGreenBackground"; // #DFF6DD
    public const string ColorRiskYellow = "--colorRiskYellow";          // #835B00
    public const string ColorRiskYellowBackground = "--colorRiskYellowBackground"; // #FFF4CE
    public const string ColorRiskRed = "--colorRiskRed";                // #A4262C
    public const string ColorRiskRedBackground = "--colorRiskRedBackground"; // #FDE7E9

    // Compliance tokens
    public const string ColorCompliant = "--colorCompliant";            // #107C10
    public const string ColorNonCompliant = "--colorNonCompliant";      // #A4262C
    public const string ColorWarning = "--colorWarning";                // #835B00

    // Spacing scale tokens (4px grid)
    public const string SpacingXS = "--spacingHorizontalXS";  // 4px
    public const string SpacingS = "--spacingHorizontalS";    // 8px
    public const string SpacingM = "--spacingHorizontalM";    // 16px
    public const string SpacingL = "--spacingHorizontalL";    // 24px
    public const string SpacingXL = "--spacingHorizontalXL";  // 32px
}
